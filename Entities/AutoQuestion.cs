using System;
using System.Collections.Generic;

namespace Olimpiadnic.Entities;

public partial class AutoQuestion
{
    public int QuestOptionId { get; set; }

    public int QuestId { get; set; }

    public string OptionText { get; set; } = null!;

    public bool IsCorrect { get; set; }

    public int SortOrder { get; set; }

    public virtual Question Quest { get; set; } = null!;
}
