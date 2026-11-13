public int Id { get; set; }
public string Username { get; set; } = string.Empty;
public string Email { get; set; } = string.Empty;
public string Role { get; set; } = "Attendee"; // "Attendee", "Organizer", "Admin"
public bool IsSuspended { get; set; } = false; // ADD THIS IF MISSING


using System;
using System.Collections.Generic;

namespace BackEnd.DatabaseModels;

public partial class User
{

public int Id { get; set; }
public string Username { get; set; } = string.Empty;
public string Email { get; set; } = string.Empty;
public string Role { get; set; } = "Attendee"; // "Attendee", "Organizer", "Admin"
public bool IsSuspended { get; set; } = false; // ADD THIS IF MISSING

    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public bool IsSuspended { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    public virtual ICollection<SecurityLog> SecurityLogs { get; set; } = new List<SecurityLog>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
