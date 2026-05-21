using System;
using System.Collections.Generic;

namespace Olimpiadnic.Entities;

public partial class QuestionAttachmentsSnapshot
{
    public int AttachSnapId { get; set; }

    public int QuestSnapshotId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public int SortOrder { get; set; }

    public virtual QuestionsSnapshot QuestSnapshot { get; set; } = null!;
}
