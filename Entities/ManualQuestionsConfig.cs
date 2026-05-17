using System;
using System.Collections.Generic;

namespace Olimpiadnic.Entities;

public partial class ManualQuestionsConfig
{
    public int QuestManualConfigId { get; set; }

    public int QuestId { get; set; }

    public int MaxScore { get; set; }

    public string? ModelAnswer { get; set; }

    public virtual Question Quest { get; set; } = null!;
}
