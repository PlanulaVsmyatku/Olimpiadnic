namespace Olimpiadnic.Models.MyOlympiads
{
    /// <summary>
    /// Общий класс для страницы "Мои олимпиады"
    /// </summary>
    public class MyOlympiads
    {
        public int OlympiadId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public DateTime EventStart { get; set; }
        public DateTime EventEnd { get; set; }
        public DateTime RegistOpen { get; set; }
        public DateTime RegistClosed { get; set; }

    }

    /// <summary>
    /// Модель для отображения олимпиад, где участник записан или имеет результаты.
    /// </summary>
    public class ParticipantMyOlympiads : MyOlympiads
    {
        public int? UserTotalScore { get; set; }
        public int MaxPossibleScore { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsRegistered { get; set; }
        public int? Percentage => MaxPossibleScore > 0 ? (UserTotalScore * 100 / MaxPossibleScore) : 0;

    }

    /// <summary>
    /// Модель для отображения олимпиад, созданных или курируемых сотрудником.
    /// </summary>
    public class StaffMyOlympiads : MyOlympiads
    {
        public bool isCreatedBy { get; set; }
        public bool isAssignedTo { get; set; }
        public int? TotalUnchekedQuestions { get; set; }
        public int? TotalParticipants { get; set; }

    }
}
