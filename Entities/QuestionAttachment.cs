using System;
using System.Collections.Generic;

namespace Olimpiadnic.Entities;

public partial class QuestionAttachment
{
    public int AttachId { get; set; }

    public int QuestId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public int SortOrder { get; set; }

    public virtual Question Quest { get; set; } = null!;
}
