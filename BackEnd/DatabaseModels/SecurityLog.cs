using System;
using System.Collections.Generic;

namespace BackEnd.DatabaseModels;

public partial class SecurityLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Action { get; set; } = null!;

    public string Details { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
