using System;
using System.Collections.Generic;

namespace Olimpiadnic.Entities;

public partial class UserProfile
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Organisation { get; set; }

    public string? PositionGrade { get; set; }

    public string Email { get; set; } = null!;

    public string? Kurator { get; set; }

    public string? ConsentFileUrl { get; set; }

    public string? City { get; set; }

    public virtual User User { get; set; } = null!;
}
