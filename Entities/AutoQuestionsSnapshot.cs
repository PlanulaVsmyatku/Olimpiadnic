using System;
using System.Collections.Generic;

namespace Olimpiadnic.Entities;

public partial class AutoQuestionsSnapshot
{
    public int QuestOptionId { get; set; }

    public int QuestSnapshotId { get; set; }

    public string OptionText { get; set; } = null!;

    public bool IsCorrect { get; set; }

    public int SortOrder { get; set; }

    public virtual QuestionsSnapshot QuestSnapshot { get; set; } = null!;
}
