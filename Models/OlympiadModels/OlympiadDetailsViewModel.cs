namespace Olimpiadnic.Models.OlympiadModels
{
    public class OlympiadDetailsViewModel
    {
        public int OlympiadId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Credentials { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime EventStart { get; set; }
        public DateTime EventEnd { get; set; }
        public DateTime RegistOpen { get; set; }
        public DateTime RegistClosed { get; set; }
        public int TotalQuestions { get; set; }
        public bool IsRegistered { get; set; }
        public bool CanParticipate { get; set; }

        public bool IsCompleted { get; set; }           // Завершена ли олимпиада пользователем
        public DateTime? CompletedAt { get; set; }      // Дата завершения
        public int? UserTotalScore { get; set; }        // Набранные баллы (если завершена)
    }
}
