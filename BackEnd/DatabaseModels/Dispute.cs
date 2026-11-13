using System;
using System.Collections.Generic;

namespace BackEnd.DatabaseModels;

public partial class Dispute
{

public int Id { get; set; }
public int TransactionId { get; set; }
public int UserId { get; set; }
public string Reason { get; set; } = string.Empty;
public string Status { get; set; } = "Open"; // "Open", "Resolved", "Refunded", "Rejected"
public string? AdminResolutionNotes { get; set; }
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int Id { get; set; }

    public int TransactionId { get; set; }

    public int UserId { get; set; }

    public string Reason { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Transaction Transaction { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
