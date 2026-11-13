using System.Security.Claims;
using BackEnd.DatabaseModels;
using BackEnd.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BackEnd;

public static class WorkflowEndpoints
{
    public static void MapWorkflowEndpoints(this WebApplication app)
    {
        // Helper to extract current_user.id from JWT claims
        static bool TryGetCurrentUserId(ClaimsPrincipal user, out int userId)
        {
            var rawId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? user.FindFirstValue("id")
                     ?? user.FindFirstValue("sub");
            return int.TryParse(rawId, out userId);
        }

        // Helper to write audit trail entries to SecurityLog
        static void WriteSecurityLog(AppDbContext db, int? userId, string action, string details)
        {
            db.SecurityLogs.Add(new SecurityLog
            {
                UserId = userId ?? 0,
                Action = action,
                Details = details,
                Timestamp = DateTime.UtcNow
            });
        }

        // =====================================================================
        // SECTION 4.2: ORGANIZER WORKFLOW
        // =====================================================================
        var organizerGroup = app.MapGroup("/api/organizer")
            .RequireAuthorization( new AuthorizeAttribute { Roles = "organizer" });

        // GET: Owned events & sales metrics (strictly filtered by OrganizerId == current_user.id)
        organizerGroup.MapGet("/events", async (ClaimsPrincipal user, AppDbContext db) =>
        {
            if (!TryGetCurrentUserId(user, out int currentUserId))
                return Results.Unauthorized();

            var ownedEvents = await db.Events
                .AsNoTracking()
                .Where(e => e.OrganizerId == currentUserId)
                .Select(e => new OrganizerEventMetricsDto
                {
                    EventId = e.Id,
                    Title = e.Title,
                    Location = e.Location,
                    EventDate = e.Date,
                    Status = e.Status,
                    Price = e.Price,
                    TotalTickets = e.TotalTickets,
                    TicketsSold = db.Tickets.Count(t => t.EventId == e.Id && t.Status != "Refunded"),
                    TotalSalesRevenue = db.Tickets
                        .Where(t => t.EventId == e.Id && t.Status != "Refunded")
                        .Sum(t => (decimal?)t.Price) ?? 0m
                })
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();

            return Results.Ok(ownedEvents);
        });

        // POST: Create new event owned by current organizer
        organizerGroup.MapPost("/events", async (EventUpsertDto dto, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (!TryGetCurrentUserId(user, out int currentUserId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Title) || dto.TotalTickets <= 0 || dto.Price < 0)
                return Results.BadRequest("Invalid event details.");

            var ev = new Event
            {
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                Location = dto.Location.Trim(),
                Date = dto.EventDate,
                TotalTickets = dto.TotalTickets,
                AvailableTickets = dto.TotalTickets,
                Price = dto.Price,
                OrganizerId = currentUserId, // Bind ownership server-side
                Status = "Active"
            };

            db.Events.Add(ev);
            await db.SaveChangesAsync();

            WriteSecurityLog(db, currentUserId, "EVENT_CREATE", $"Organizer {currentUserId} created event {ev.Id} ({ev.Title}).");
            await db.SaveChangesAsync();

            return Results.Created($"/api/organizer/events/{ev.Id}", ev);
        });

        // PUT: Update event (Server-side verification: event.OrganizerId == current_user.id)
        organizerGroup.MapPut("/events/{id:int}", async (int id, EventUpsertDto dto, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (!TryGetCurrentUserId(user, out int currentUserId))
                return Results.Unauthorized();

            var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null)
                return Results.NotFound();

            // Resource-level authorization check
            if (ev.OrganizerId != currentUserId)
            {
                WriteSecurityLog(db, currentUserId, "UNAUTHORIZED_EVENT_UPDATE",
                    $"User {currentUserId} attempted to update Event {id} owned by {ev.OrganizerId}.");
                await db.SaveChangesAsync();
                return Results.Forbid();
            }

            int soldCount = await db.Tickets.CountAsync(t => t.EventId == id && t.Status != "Refunded");
            if (dto.TotalTickets < soldCount)
                return Results.BadRequest($"Total tickets cannot be less than already sold tickets ({soldCount}).");

            ev.Title = dto.Title.Trim();
            ev.Description = dto.Description.Trim();
            ev.Location = dto.Location.Trim();
            ev.Date = dto.EventDate;
            ev.TotalTickets = dto.TotalTickets;
            ev.AvailableTickets = dto.TotalTickets - soldCount;
            ev.Price = dto.Price;

            WriteSecurityLog(db, currentUserId, "EVENT_UPDATE", $"Organizer {currentUserId} updated Event {ev.Id}.");
            await db.SaveChangesAsync();

            return Results.Ok(ev);
        });

        // DELETE: Delete event (Server-side verification: event.OrganizerId == current_user.id)
        organizerGroup.MapDelete("/events/{id:int}", async (int id, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (!TryGetCurrentUserId(user, out int currentUserId))
                return Results.Unauthorized();

            var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null)
                return Results.NotFound();

            // Resource-level authorization check
            if (ev.OrganizerId != currentUserId)
            {
                WriteSecurityLog(db, currentUserId, "UNAUTHORIZED_EVENT_DELETE",
                    $"User {currentUserId} attempted to delete Event {id} owned by {ev.OrganizerId}.");
                await db.SaveChangesAsync();
                return Results.Forbid();
            }

            db.Events.Remove(ev);
            WriteSecurityLog(db, currentUserId, "EVENT_DELETE", $"Organizer {currentUserId} deleted Event {id} ({ev.Title}).");
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        // =====================================================================
        // SECTION 4.3: ADMINISTRATOR WORKFLOW (LEAST PRIVILEGE)
        // =====================================================================
        var adminGroup = app.MapGroup("/api/admin")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "admin" });

        // GET: View users (least privilege: never expose password hashes)
        adminGroup.MapGet("/users", async (AppDbContext db) =>
        {
            var users = await db.Users
                .AsNoTracking()
                .Select(u => new AdminUserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Role = u.Role,
                    IsSuspended = u.IsSuspended
                })
                .OrderBy(u => u.Id)
                .ToListAsync();

            return Results.Ok(users);
        });

        // PUT: Suspend or unsuspend a user
        adminGroup.MapPut("/users/{id:int}/suspend", async (int id, ClaimsPrincipal adminUser, AppDbContext db) =>
        {
            if (!TryGetCurrentUserId(adminUser, out int adminId))
                return Results.Unauthorized();

            if (id == adminId)
                return Results.BadRequest("Administrators cannot suspend their own account.");

            var targetUser = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (targetUser == null)
                return Results.NotFound();

            targetUser.IsSuspended = !targetUser.IsSuspended;
            string state = targetUser.IsSuspended ? "SUSPENDED" : "REACTIVATED";

            WriteSecurityLog(db, adminId, $"USER_{state}", $"Admin {adminId} {state.ToLower()} User {targetUser.Id} ({targetUser.Username}).");
            await db.SaveChangesAsync();

            return Results.Ok(new AdminUserDto
            {
                Id = targetUser.Id,
                Username = targetUser.Username,
                Role = targetUser.Role,
                IsSuspended = targetUser.IsSuspended
            });
        });

        // GET: System-wide event management view
        adminGroup.MapGet("/events", async (AppDbContext db) =>
        {
            var events = await db.Events
                .AsNoTracking()
                .Select(e => new AdminEventDto
                {
                    Id = e.Id,
                    OrganizerId = e.OrganizerId,
                    OrganizerUsername = db.Users
                        .Where(u => u.Id == e.OrganizerId)
                        .Select(u => u.Username)
                        .FirstOrDefault() ?? "Unknown",
                    Title = e.Title,
                    EventDate = e.Date,
                    Status = e.Status,
                    Price = e.Price,
                    TicketsSold = db.Tickets.Count(t => t.EventId == e.Id && t.Status != "Refunded")
                })
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();

            return Results.Ok(events);
        });

        // PUT: Administrative event cancellation & automatic ticket refund status
        adminGroup.MapPut("/events/{id:int}/cancel", async (int id, ClaimsPrincipal adminUser, AppDbContext db) =>
        {
            if (!TryGetCurrentUserId(adminUser, out int adminId))
                return Results.Unauthorized();

            var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null)
                return Results.NotFound();

            ev.Status = "Cancelled";
            ev.AvailableTickets = 0;

            var activeTickets = await db.Tickets
                .Where(t => t.EventId == id && t.Status != "Refunded")
                .ToListAsync();

            foreach (var ticket in activeTickets)
            {
                ticket.Status = "Refunded";
            }

            WriteSecurityLog(db, adminId, "ADMIN_EVENT_CANCEL",
                $"Admin {adminId} cancelled Event {id} ({ev.Title}) and refunded {activeTickets.Count} tickets.");
            await db.SaveChangesAsync();

            return Results.Ok();
        });

        // GET: View all disputes under least privilege
        adminGroup.MapGet("/disputes", async (AppDbContext db) =>
        {
            var disputes = await db.Disputes
                .AsNoTracking()
                .OrderBy(d => d.Status == "Open" ? 0 : 1)
                .ThenByDescending(d => d.CreatedAt)
                .Select(d => new AdminDisputeDto
                {
                    Id = d.Id,
                    TransactionId = d.TransactionId,
                    EventId = d.EventId,
                    EventTitle = db.Events
                        .Where(e => e.Id == d.EventId)
                        .Select(e => e.Title)
                        .FirstOrDefault() ?? "N/A",
                    UserId = d.UserId,
                    Reason = d.Reason,
                    Status = d.Status,
                    ResolutionNotes = d.ResolutionNotes,
                    CreatedAt = d.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(disputes);
        });

        // PUT: Handle dispute resolution (Approve Refund or Reject)
        adminGroup.MapPut("/disputes/{id:int}/resolve", async (int id, ResolveDisputeRequest req, ClaimsPrincipal adminUser, AppDbContext db) =>
        {
            if (!TryGetCurrentUserId(adminUser, out int adminId))
                return Results.Unauthorized();

            var dispute = await db.Disputes.FirstOrDefaultAsync(d => d.Id == id);
            if (dispute == null)
                return Results.NotFound();

            if (dispute.Status != "Open")
                return Results.BadRequest("Dispute is already resolved.");

            bool approveRefund = string.Equals(req.Action, "Refund", StringComparison.OrdinalIgnoreCase);
            dispute.Status = approveRefund ? "Resolved - Refunded" : "Rejected";
            dispute.ResolutionNotes = req.ResolutionNotes;

            if (approveRefund)
            {
                var tickets = await db.Tickets
                    .Where(t => t.TransactionId == dispute.TransactionId)
                    .ToListAsync();

                foreach (var t in tickets)
                {
                    t.Status = "Refunded";
                }
            }

            WriteSecurityLog(db, adminId, "ADMIN_DISPUTE_RESOLVE",
                $"Admin {adminId} resolved Dispute {id} with status '{dispute.Status}'. Notes: {req.ResolutionNotes}");
            await db.SaveChangesAsync();

            return Results.Ok();
        });
    }
}