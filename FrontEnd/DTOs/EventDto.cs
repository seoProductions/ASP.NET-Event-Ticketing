namespace FrontEnd.DTOs;

public class EventDto
{
    public int Id { get; set; }
    public int OrganizerId { get; set; }

    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Location { get; set; } = "";

    public DateTime EventDate { get; set; }

    public decimal Price { get; set; }

    public int TotalTickets { get; set; }
    public int AvailableTickets { get; set; }

    public DateTime CreatedAt { get; set; }
}