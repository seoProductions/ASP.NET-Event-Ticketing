using System;
using System.Collections.Generic;

namespace BackEnd.DatabaseModels;

public partial class SecurityLog
{

public int Id { get; set; }
public int ActorUserId { get; set; }
public string Action { get; set; } = string.Empty;
public string TargetEntity { get; set; } = string.Empty;
public int TargetId { get; set; }
public string Details { get; set; } = string.Empty;
public DateTime Timestamp { get; set; } = DateTime.UtcNow;


    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Action { get; set; } = null!;

    public string Details { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
