using System;
using System.Collections.Generic;

namespace Olimpiadnic.Entities;

public partial class SubmissionItem
{
    public int SubmissionItemsId { get; set; }

    public int ResultsId { get; set; }

    public int QuestSnapshotId { get; set; }

    public string Type { get; set; } = null!;

    public virtual ICollection<AutoSubmissionResult> AutoSubmissionResults { get; set; } = new List<AutoSubmissionResult>();

    public virtual ManualSubmissionResult? ManualSubmissionResult { get; set; }

    public virtual QuestionsSnapshot QuestSnapshot { get; set; } = null!;

    public virtual OlympiadResult Results { get; set; } = null!;
}
