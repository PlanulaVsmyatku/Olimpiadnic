using System;
using System.Collections.Generic;

namespace Olimpiadnic.Entities;

public partial class QuestionsSnapshot
{
    public int QuestSnapshotId { get; set; }

    public int OlympSnapId { get; set; }

    public int? QuestIdOriginal { get; set; }

    public int QuestOrderSnapshot { get; set; }

    public string QuestionDescSnapshot { get; set; } = null!;

    public string QuestionTypeSnapshot { get; set; } = null!;

    public virtual ICollection<AutoQuestionsSnapshot> AutoQuestionsSnapshots { get; set; } = new List<AutoQuestionsSnapshot>();

    public virtual ICollection<ManualQuestionsConfigSnapshot> ManualQuestionsConfigSnapshots { get; set; } = new List<ManualQuestionsConfigSnapshot>();

    public virtual OlympiadSnapshot OlympSnap { get; set; } = null!;

    public virtual ICollection<SubmissionItem> SubmissionItems { get; set; } = new List<SubmissionItem>();
}
