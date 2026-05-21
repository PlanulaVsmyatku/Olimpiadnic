using System.ComponentModel.DataAnnotations;

namespace Olimpiadnic.Models.OlympiadModels
{
    #region Прохождение олимпиады

    public class OlympiadParticipationViewModel
    {
        public int OlympiadId { get; set; }
        public string OlympiadTitle { get; set; } = string.Empty;
        public int ParticipantId { get; set; }
        public int CurrentQuestionIndex { get; set; }
        public int TotalQuestions { get; set; }
        public bool IsCompleted { get; set; }

        // Список всех вопросов для хранения ответов в сессии
        public List<QuestionParticipationViewModel> Questions { get; set; } = new();

        // Текущий вопрос (для удобства)
        public QuestionParticipationViewModel CurrentQuestion =>
            Questions.ElementAtOrDefault(CurrentQuestionIndex) ?? new QuestionParticipationViewModel();
    }

    public class QuestionParticipationViewModel
    {
        public int QuestionId { get; set; }
        public int OrderNumber { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // auto или manual
        public List<string> Attachments { get; set; } = new();

        // Для авто-вопросов
        public List<AutoQuestionOptionParticipationViewModel> Options { get; set; } = new();

        // Для ручных вопросов
        public string? AnswerText { get; set; }

        // Хранилище ответов пользователя
        public List<int> SelectedOptionIds { get; set; } = new(); // для radio/checkbox
        public string ManualAnswer { get; set; } = string.Empty;
    }

    public class AutoQuestionOptionParticipationViewModel
    {
        public int OptionId { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    #endregion
}