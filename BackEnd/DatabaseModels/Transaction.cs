using System;
using System.Collections.Generic;

namespace BackEnd.DatabaseModels;

public partial class Transaction
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int EventId { get; set; }

    public int Quantity { get; set; }

    public decimal TotalPrice { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public string? PaymentRef { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();

    public virtual Event Event { get; set; } = null!;

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual User User { get; set; } = null!;
}
