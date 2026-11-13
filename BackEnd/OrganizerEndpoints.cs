using System.Security.Claims;
using BackEnd.DatabaseModels;
using BackEnd.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BackEnd;

public static class OrganizerEndpoints
{
    public static void MapOrganizerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/organizer")
                       .RequireAuthorization(policy => policy.RequireRole("Organizer", "Admin"));

        // Helper to extract current_user.id from JWT
        static bool TryGetCurrentUserId(ClaimsPrincipal user, out int userId)
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                       ?? user.FindFirst("sub")?.Value 
                       ?? user.FindFirst("id")?.Value;
            return int.TryParse(idClaim, out userId);
        }

        // 1. GET OWNED EVENTS + METRICS (Ticket counts, pricing, sales view for owned events ONLY)
        group.MapGet("/events", async (ClaimsPrincipal user, AppDbContext db) =>
        {
            if (!TryGetCurrentUserId(user, out int currentUserId))
                return Results.Unauthorized();

            var ownedEvents = await db.Events
                .Where(e => e.OrganizerId == currentUserId)
                .Select(e => new OrganizerEventMetricsDto
                {
                    EventId = e.Id,
                    Title = e.Title,
                    EventDate = e.EventDate,
                    Location = e.Location,
                    TicketPrice = e.TicketPrice,
                    TotalTickets = e.TotalTickets,
                    IsCancelled = e.IsCancelled,
                    // Count sold tickets tied to this event
                    TicketsSold = db.Tickets.Count(t => t.EventId == e.Id)
                })
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();

            return Results.Ok(ownedEvents);
        });

        // 2. GET SINGLE OWNED EVENT FOR EDITING
        group.MapGet("/events/{id:int}", async (int id, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (!TryGetCurrentUserId(user, out int currentUserId))
                return Results.Unauthorized();

            var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (ev is null) return Results.NotFound("Event not found.");

            // Resource-level authorization check
            if (ev.OrganizerId != currentUserId)
                return Results.Forbid();

            return Results.Ok(ev);
        });

        // 3. CREATE EVENT
        group.MapPost("/events", async (UpsertEventRequest req, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (!TryGetCurrentUserId(user, out int currentUserId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(req.Title) || req.TotalTickets <= 0 || req.TicketPrice < 0)
                return Results.BadRequest("Invalid event details.");

            var newEvent = new Event
            {
                OrganizerId = currentUserId,
                Title = req.Title.Trim(),
                Description = req.Description.Trim(),
                Location = req.Location.Trim(),
                EventDate = req.EventDate,
                TicketPrice = req.TicketPrice,
                TotalTickets = req.TotalTickets,
                IsCancelled = false
            };

            db.Events.Add(newEvent);
            await db.SaveChangesAsync();

            db.SecurityLogs.Add(new SecurityLog
            {
                ActorUserId = currentUserId,
                Action = "CREATE_EVENT",
                TargetEntity = "Event",
                TargetId = newEvent.Id,
                Details = $"Organizer {currentUserId} created event '{newEvent.Title}'.",
                Timestamp = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            return Results.Created($"/api/organizer/events/{newEvent.Id}", newEvent);
        });

        // 4. UPDATE EVENT (Server-side verification: event.OrganizerId == currentUserId)
        group.MapPut("/events/{id:int}", async (int id, UpsertEventRequest req, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (!TryGetCurrentUserId(user, out int currentUserId))
                return Results.Unauthorized();

            var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (ev is null) return Results.NotFound("Event not found.");

            // CRITICAL: Resource-level authorization
            if (ev.OrganizerId != currentUserId)
            {
                db.SecurityLogs.Add(new SecurityLog
                {
                    ActorUserId = currentUserId,
                    Action = "UNAUTHORIZED_UPDATE_ATTEMPT",
                    TargetEntity = "Event",
                    TargetId = id,
                    Details = $"User {currentUserId} attempted to update Event {id} owned by {ev.OrganizerId}.",
                    Timestamp = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
                return Results.Forbid();
            }

            ev.Title = req.Title.Trim();
            ev.Description = req.Description.Trim();
            ev.Location = req.Location.Trim();
            ev.EventDate = req.EventDate;
            ev.TicketPrice = req.TicketPrice;
            ev.TotalTickets = req.TotalTickets;

            db.SecurityLogs.Add(new SecurityLog
            {
                ActorUserId = currentUserId,
                Action = "UPDATE_EVENT",
                TargetEntity = "Event",
                TargetId = ev.Id,
                Details = $"Organizer {currentUserId} updated event '{ev.Title}'.",
                Timestamp = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            return Results.Ok(ev);
        });

        // 5. DELETE EVENT (Server-side verification: event.OrganizerId == currentUserId)
        group.MapDelete("/events/{id:int}", async (int id, ClaimsPrincipal user, AppDbContext db) =>
        {
            if (!TryGetCurrentUserId(user, out int currentUserId))
                return Results.Unauthorized();

            var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (ev is null) return Results.NotFound("Event not found.");

            // CRITICAL: Resource-level authorization
            if (ev.OrganizerId != currentUserId)
            {
                db.SecurityLogs.Add(new SecurityLog
                {
                    ActorUserId = currentUserId,
                    Action = "UNAUTHORIZED_DELETE_ATTEMPT",
                    TargetEntity = "Event",
                    TargetId = id,
                    Details = $"User {currentUserId} attempted to delete Event {id} owned by {ev.OrganizerId}.",
                    Timestamp = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
                return Results.Forbid();
            }

            db.Events.Remove(ev);

            db.SecurityLogs.Add(new SecurityLog
            {
                ActorUserId = currentUserId,
                Action = "DELETE_EVENT",
                TargetEntity = "Event",
                TargetId = id,
                Details = $"Organizer {currentUserId} deleted event '{ev.Title}'.",
                Timestamp = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
