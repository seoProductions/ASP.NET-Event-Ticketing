


using System;
using System.Collections.Generic;

namespace BackEnd.DatabaseModels;

public partial class Event
{

public int Id { get; set; }
public int OrganizerId { get; set; } // Maps to event.organizer_id
public string Title { get; set; } = string.Empty;
public string Description { get; set; } = string.Empty;
public string Location { get; set; } = string.Empty;
public DateTime EventDate { get; set; }
public decimal TicketPrice { get; set; }
public int TotalTickets { get; set; }
public bool IsCancelled { get; set; } = false; // ADD THIS IF MISSING


    public int Id { get; set; }

    public int OrganizerId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string Location { get; set; } = null!;

    public DateTime EventDate { get; set; }

    public decimal Price { get; set; }

    public int TotalTickets { get; set; }

    public int AvailableTickets { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User Organizer { get; set; } = null!;

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
