using System;
using System.Collections.Generic;

namespace Olimpiadnic.Entities;

public partial class Invite
{
    public int InviteId { get; set; }

    public string Token { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int? RoleId { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsUsed { get; set; }
}
