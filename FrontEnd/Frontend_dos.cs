namespace FrontEnd.DTOs; // Use BackEnd.DTOs in the BackEnd project

public class EventUpsertDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime EventDate { get; set; } = DateTime.UtcNow.AddDays(7);
    public int TotalTickets { get; set; }
    public decimal Price { get; set; }
}

public class OrganizerEventMetricsDto
{
    public int EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Status { get; set; } = "Active";
    public decimal Price { get; set; }
    public int TotalTickets { get; set; }
    public int TicketsSold { get; set; }
    public int TicketsRemaining => TotalTickets - TicketsSold;
    public decimal TotalSalesRevenue { get; set; }
}

public class AdminUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsSuspended { get; set; }
}

public class AdminEventDto
{
    public int Id { get; set; }
    public int OrganizerId { get; set; }
    public string OrganizerUsername { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Status { get; set; } = "Active";
    public int TicketsSold { get; set; }
    public decimal Price { get; set; }
}

public class AdminDisputeDto
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public string? ResolutionNotes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ResolveDisputeRequest
{
    public string Action { get; set; } = "Refund"; // "Refund" or "Reject"
    public string ResolutionNotes { get; set; } = string.Empty;
}