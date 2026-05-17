using System;
using System.Collections.Generic;

namespace Olimpiadnic.Entities;

public partial class Olympiad
{
    public int OlympId { get; set; }

    public string Title { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public string? Description { get; set; }

    public string? Credentials { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime EventStart { get; set; }

    public DateTime EventEnd { get; set; }

    public DateTime RegistOpen { get; set; }

    public DateTime RegistClosed { get; set; }

    public virtual ICollection<OlympStaff> OlympStaffs { get; set; } = new List<OlympStaff>();

    public virtual ICollection<OlympiadParticipant> OlympiadParticipants { get; set; } = new List<OlympiadParticipant>();

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}
