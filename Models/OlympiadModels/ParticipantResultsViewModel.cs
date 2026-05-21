namespace Olimpiadnic.Models.OlympiadModels
{
    public class ParticipantResultsViewModel
    {
        public int OlympiadId { get; set; }
        public string OlympiadTitle { get; set; } = string.Empty;
        public string ParticipantName { get; set; } = string.Empty;
        public int TotalScore { get; set; }
        public int MaxPossibleScore { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = string.Empty;

        public int AutoScore { get; set; }
        public int ManualScore { get; set; }

        public List<QuestionResultViewModel> QuestionResults { get; set; } = new();

        public int Percentage => MaxPossibleScore > 0 ? (TotalScore * 100 / MaxPossibleScore) : 0;
        public TimeSpan Duration => (CompletedAt - StartedAt) ?? TimeSpan.Zero;
    }

    public class QuestionResultViewModel
    {
        public int QuestionId { get; set; }
        public int OrderNumber { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public string UserAnswer { get; set; } = string.Empty;
        public int? Score { get; set; }
        public int MaxScore { get; set; }
        public bool? IsCorrect { get; set; }
        public string? Commentary { get; set; }
        public string Status { get; set; } = "pending"; // correct, incorrect, pending, reviewed

        public string StatusText => Status switch
        {
            "correct" => "✓ Верно",
            "incorrect" => "✗ Неверно",
            "pending" => "⏳ Ожидает проверки",
            "reviewed" => "✓ Проверено",
            _ => "—"
        };

        public string StatusClass => Status switch
        {
            "correct" => "text-success",
            "incorrect" => "text-danger",
            "pending" => "text-warning",
            "reviewed" => "text-info",
            _ => "text-muted"
        };
    }
}
