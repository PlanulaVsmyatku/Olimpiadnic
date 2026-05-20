using System;
using System.Collections.Generic;

namespace Olimpiadnic.Entities;

public partial class User
{
    public int UserId { get; set; }

    public string Login { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public bool IsActivated { get; set; }

    public DateTime? LastLogin { get; set; }

    public virtual ICollection<OlympStaff> OlympStaffs { get; set; } = new List<OlympStaff>();

    public virtual ICollection<OlympiadParticipant> OlympiadParticipants { get; set; } = new List<OlympiadParticipant>();

    public virtual UserProfile? UserProfile { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
