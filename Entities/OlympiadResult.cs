using System;
using System.Collections.Generic;

namespace Olimpiadnic.Entities;

public partial class OlympiadResult
{
    public int ResultsId { get; set; }

    public int ParticipantId { get; set; }

    public int? TotalScore { get; set; }

    public virtual OlympiadParticipant Participant { get; set; } = null!;

    public virtual ICollection<SubmissionItem> SubmissionItems { get; set; } = new List<SubmissionItem>();
}
