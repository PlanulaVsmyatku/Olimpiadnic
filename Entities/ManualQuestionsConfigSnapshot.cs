using System;
using System.Collections.Generic;

namespace Olimpiadnic.Entities;

public partial class ManualQuestionsConfigSnapshot
{
    public int QuestManualConfigId { get; set; }

    public int QuestSnapshotId { get; set; }

    public int MaxScore { get; set; }

    public string? ModelAnswer { get; set; }

    public virtual QuestionsSnapshot QuestSnapshot { get; set; } = null!;
}
