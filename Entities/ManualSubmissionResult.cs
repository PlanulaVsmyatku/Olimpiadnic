using System;
using System.Collections.Generic;

namespace Olimpiadnic.Entities;

public partial class ManualSubmissionResult
{
    public int SubmissionItemId { get; set; }

    public string AnswerText { get; set; } = null!;

    public int? ScoreValue { get; set; }

    public string? Commentary { get; set; }

    public virtual SubmissionItem SubmissionItem { get; set; } = null!;
}
