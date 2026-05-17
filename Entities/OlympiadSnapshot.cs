using System;
using System.Collections.Generic;

namespace Olimpiadnic.Entities;

public partial class OlympiadSnapshot
{
    public int OlympSnapId { get; set; }

    public string Title { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime CreatedAtSnap { get; set; }

    public string? Credentials { get; set; }

    public string Status { get; set; } = null!;

    public DateTime EventStart { get; set; }

    public DateTime EventEnd { get; set; }

    public DateTime RegistOpen { get; set; }

    public DateTime RegistClosed { get; set; }

    public virtual ICollection<QuestionsSnapshot> QuestionsSnapshots { get; set; } = new List<QuestionsSnapshot>();
}
