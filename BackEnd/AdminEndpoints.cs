using System.Security.Claims;
using BackEnd.DatabaseModels;
using BackEnd.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BackEnd;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin")
                       .RequireAuthorization(policy => policy.RequireRole("Admin"));

        static int GetAdminId(ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
            return int.TryParse(claim, out int id) ? id : 0;
        }

        // 1. VIEW USERS (Least Privilege projection: excludes password hashes/secrets)
        group.MapGet("/users", async (AppDbContext db) =>
        {
            var users = await db.Users
                .Select(u => new AdminUserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role,
                    IsSuspended = u.IsSuspended
                })
                .ToListAsync();

            return Results.Ok(users);
        });

        // 2. SUSPEND / UNSUSPEND USER
        group.MapPut("/users/{id:int}/toggle-suspension", async (int id, ClaimsPrincipal admin, AppDbContext db) =>
        {
            int adminId = GetAdminId(admin);
            if (adminId == id)
                return Results.BadRequest("Administrators cannot suspend their own account.");

            var targetUser = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (targetUser is null) return Results.NotFound("User not found.");

            targetUser.IsSuspended = !targetUser.IsSuspended;

            db.SecurityLogs.Add(new SecurityLog
            {
                ActorUserId = adminId,
                Action = targetUser.IsSuspended ? "ADMIN_SUSPEND_USER" : "ADMIN_UNSUSPEND_USER",
                TargetEntity = "User",
                TargetId = targetUser.Id,
                Details = $"Admin {adminId} set IsSuspended={targetUser.IsSuspended} for user '{targetUser.Username}'.",
                Timestamp = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            return Results.Ok(new { targetUser.Id, targetUser.IsSuspended });
        });

        // 3. SYSTEM-WIDE EVENT VIEW
        group.MapGet("/events", async (AppDbContext db) =>
        {
            var events = await db.Events
                .Select(e => new OrganizerEventMetricsDto
                {
                    EventId = e.Id,
                    Title = e.Title,
                    EventDate = e.EventDate,
                    Location = e.Location,
                    TicketPrice = e.TicketPrice,
                    TotalTickets = e.TotalTickets,
                    IsCancelled = e.IsCancelled,
                    TicketsSold = db.Tickets.Count(t => t.EventId == e.Id)
                })
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();

            return Results.Ok(events);
        });

        // 4. ADMINISTRATIVE EVENT CANCELLATION (Soft-cancel rather than destructive wipe to preserve transaction/refund integrity)
        group.MapPut("/events/{id:int}/cancel", async (int id, ClaimsPrincipal admin, AppDbContext db) =>
        {
            int adminId = GetAdminId(admin);
            var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (ev is null) return Results.NotFound("Event not found.");

            ev.IsCancelled = true;

            db.SecurityLogs.Add(new SecurityLog
            {
                ActorUserId = adminId,
                Action = "ADMIN_CANCEL_EVENT",
                TargetEntity = "Event",
                TargetId = ev.Id,
                Details = $"Admin {adminId} cancelled event '{ev.Title}'.",
                Timestamp = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            return Results.Ok(new { ev.Id, ev.IsCancelled });
        });

        // 5. VIEW DISPUTES
        group.MapGet("/disputes", async (AppDbContext db) =>
        {
            var disputes = await db.Disputes
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
            return Results.Ok(disputes);
        });

        // 6. HANDLE / RESOLVE DISPUTE UNDER LEAST PRIVILEGE
        group.MapPut("/disputes/{id:int}/resolve", async (int id, ResolveDisputeRequest req, ClaimsPrincipal admin, AppDbContext db) =>
        {
            int adminId = GetAdminId(admin);
            var dispute = await db.Disputes.FirstOrDefaultAsync(d => d.Id == id);
            if (dispute is null) return Results.NotFound("Dispute not found.");

            if (dispute.Status != "Open")
                return Results.BadRequest("Only open disputes can be modified.");

            dispute.Status = req.Status;
            dispute.AdminResolutionNotes = req.ResolutionNotes;

            db.SecurityLogs.Add(new SecurityLog
            {
                ActorUserId = adminId,
                Action = "ADMIN_RESOLVE_DISPUTE",
                TargetEntity = "Dispute",
                TargetId = dispute.Id,
                Details = $"Admin {adminId} marked Dispute {dispute.Id} as '{req.Status}'. Notes: {req.ResolutionNotes}",
                Timestamp = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            return Results.Ok(dispute);
        });

        // 7. VIEW SECURITY AUDIT LOGS (Read-only)
        group.MapGet("/security-logs", async (AppDbContext db) =>
        {
            var logs = await db.SecurityLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(200)
                .ToListAsync();
            return Results.Ok(logs);
        });
    }
}