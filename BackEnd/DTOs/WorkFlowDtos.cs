namespace BackEnd.DTOs;

public class UpsertEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public decimal TicketPrice { get; set; }
    public int TotalTickets { get; set; }
}

public class OrganizerEventMetricsDto
{
    public int EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal TicketPrice { get; set; }
    public int TotalTickets { get; set; }
    public int TicketsSold { get; set; }
    public int TicketsRemaining => Math.Max(0, TotalTickets - TicketsSold);
    public decimal GrossSales => TicketsSold * TicketPrice;
    public bool IsCancelled { get; set; }
}

public class AdminUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsSuspended { get; set; }
}

public class ResolveDisputeRequest
{
    public string Status { get; set; } = "Resolved"; // "ApprovedRefund" or "Rejected"
    public string ResolutionNotes { get; set; } = string.Empty;
    public bool CancelAssociatedEvent { get; set; } = false;
}