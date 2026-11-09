using System;
using System.Collections.Generic;

namespace BackEnd.DatabaseModels;

public partial class Ticket
{
    public int Id { get; set; }

    public int TransactionId { get; set; }

    public int EventId { get; set; }

    public int UserId { get; set; }

    public string TicketCode { get; set; } = null!;

    public string Status { get; set; } = null!;

    public virtual Event Event { get; set; } = null!;

    public virtual Transaction Transaction { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
