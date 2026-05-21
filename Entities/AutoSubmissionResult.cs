using System;
using System.Collections.Generic;

namespace Olimpiadnic.Entities;

public partial class AutoSubmissionResult
{
    public int SubmissionItemId { get; set; }

    public int SelectedOptionId { get; set; }

    public bool? IsCorrect { get; set; }

    public virtual SubmissionItem SubmissionItem { get; set; } = null!;
}
