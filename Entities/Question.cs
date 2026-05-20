using System;
using System.Collections.Generic;

namespace Olimpiadnic.Entities;

public partial class Question
{
    public int QuestId { get; set; }

    public int OlympId { get; set; }

    public int QuestionOrder { get; set; }

    public string Description { get; set; } = null!;

    public string Type { get; set; } = null!;

    public bool IsActual { get; set; }

    public virtual ICollection<AutoQuestion> AutoQuestions { get; set; } = new List<AutoQuestion>();

    public virtual ICollection<ManualQuestionsConfig> ManualQuestionsConfigs { get; set; } = new List<ManualQuestionsConfig>();

    public virtual Olympiad Olymp { get; set; } = null!;

    public virtual ICollection<QuestionAttachment> QuestionAttachments { get; set; } = new List<QuestionAttachment>();
}
