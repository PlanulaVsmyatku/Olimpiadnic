using System;
using System.Collections.Generic;

namespace Olimpiadnic.Entities;

public partial class OlympiadParticipant
{
    public int ParticipantId { get; set; }

    public int UserId { get; set; }

    public int OlympId { get; set; }

    public DateTime RegDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual Olympiad Olymp { get; set; } = null!;

    public virtual ICollection<OlympiadResult> OlympiadResults { get; set; } = new List<OlympiadResult>();

    public virtual User User { get; set; } = null!;
}
