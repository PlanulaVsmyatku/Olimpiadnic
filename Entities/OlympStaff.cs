using System;
using System.Collections.Generic;

namespace Olimpiadnic.Entities;

public partial class OlympStaff
{
    public int StaffId { get; set; }

    public int OlympId { get; set; }

    public int UserId { get; set; }

    public string OlimpRole { get; set; } = null!;

    public DateTime AssignedAt { get; set; }

    public virtual Olympiad Olymp { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
