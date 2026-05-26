//StaffModels.cs
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Olimpiadnic.Models.RoleModels 
{
    /// <summary>
    /// Модель для создания/редактирования олимпиады с черновиками
    /// </summary>
    public class CreateOlympiadViewModel
    {
        public int OlympiadId { get; set; }
        public bool IsEditMode { get; set; }

        [Required(ErrorMessage = "Введите название олимпиады")]
        [Display(Name = "Название олимпиады")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Название должно быть от 3 до 200 символов")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Изображение олимпиады")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Загрузить изображение")]
        [JsonIgnore]
        public IFormFile? ImageFile { get; set; }

        [Required(ErrorMessage = "Введите описание олимпиады")]
        [Display(Name = "Описание")]
        [StringLength(5000, MinimumLength = 10, ErrorMessage = "Описание должно быть от 10 до 5000 символов")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Критерии допуска")]
        [StringLength(1000, ErrorMessage = "Критерии не должны превышать 1000 символов")]
        public string? Credentials { get; set; }

        [Required(ErrorMessage = "Укажите дату начала регистрации")]
        [Display(Name = "Начало регистрации")]
        [DataType(DataType.DateTime)]
        public DateTime RegistOpen { get; set; }

        [Required(ErrorMessage = "Укажите дату окончания регистрации")]
        [Display(Name = "Окончание регистрации")]
        [DataType(DataType.DateTime)]
        public DateTime RegistClosed { get; set; }

        [Required(ErrorMessage = "Укажите дату начала олимпиады")]
        [Display(Name = "Начало олимпиады")]
        [DataType(DataType.DateTime)]
        public DateTime EventStart { get; set; }

        [Required(ErrorMessage = "Укажите дату окончания олимпиады")]
        [Display(Name = "Окончание олимпиады")]
        [DataType(DataType.DateTime)]
        public DateTime EventEnd { get; set; }

        public string Status { get; set; } = "available";

        // Текущий индекс вопроса для редактирования (0-based)
        public int CurrentQuestionIndex { get; set; } = 0;

        public List<QuestionEditorViewModel> Questions { get; set; } = new();
    }

    /// <summary>
    /// Модель вопроса в редакторе
    /// </summary>
    public class QuestionEditorViewModel
    {
        public int TempId { get; set; }
        public int? QuestionId { get; set; } // Оригинальный ID (если редактирование)
        public int OrderNumber { get; set; }

        [Required(ErrorMessage = "Введите текст вопроса")]
        [Display(Name = "Текст вопроса")]
        [StringLength(2000, MinimumLength = 5, ErrorMessage = "Текст должен быть от 5 до 2000 символов")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Выберите тип вопроса")]
        public string Type { get; set; } = "manual"; // manual, auto-radio, auto-checkbox

        public bool IsActual { get; set; } = true;
        public bool IsExpanded { get; set; } = true; // Для UI

        // Для авто-вопросов
        public List<AutoQuestionOptionEditorViewModel> Options { get; set; } = new();

        // Для ручных вопросов
        [Range(1, 100, ErrorMessage = "Максимальный балл должен быть от 1 до 100")]
        public int? MaxScore { get; set; }

        [Display(Name = "Эталонный ответ")]
        [StringLength(5000, ErrorMessage = "Эталон не должен превышать 5000 символов")]
        public string? ModelAnswer { get; set; }

        // Вложения
        public List<QuestionAttachmentEditorViewModel> Attachments { get; set; } = new();

        // Для отображения в пагинации
        public bool IsSaved => !string.IsNullOrWhiteSpace(Description) && Description.Length > 10;
        public string ShortTitle => string.IsNullOrEmpty(Description)
            ? $"Вопрос {OrderNumber}"
            : (Description.Length > 40 ? Description.Substring(0, 40) + "..." : Description);
    }

    /// <summary>
    /// Модель варианта ответа для авто-вопроса
    /// </summary>
    public class AutoQuestionOptionEditorViewModel
    {
        public int TempId { get; set; }
        public int? OptionId { get; set; }

        [Required(ErrorMessage = "Введите текст варианта")]
        public string OptionText { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }
        public int SortOrder { get; set; }
    }

    /// <summary>
    /// Модель вложения
    /// </summary>
    public class QuestionAttachmentEditorViewModel
    {
        public int TempId { get; set; }
        public int? AttachmentId { get; set; }
        public string? ImageUrl { get; set; }
        [JsonIgnore]
        public IFormFile? ImageFile { get; set; }
        public int SortOrder { get; set; }
    }

    /// <summary>
    /// DTO для сохранения черновика в сессии
    /// </summary>
    public class OlympiadDraftDto
    {
        public int DraftId { get; set; }
        public int UserId { get; set; }
        public int? OlympiadId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Credentials { get; set; }
        public DateTime RegistOpen { get; set; }
        public DateTime RegistClosed { get; set; }
        public DateTime EventStart { get; set; }
        public DateTime EventEnd { get; set; }
        public string Status { get; set; } = "available";
        public DateTime LastSaved { get; set; }
        public List<QuestionDraftDto> Questions { get; set; } = new();
    }

    public class QuestionDraftDto
    {
        public int TempId { get; set; }
        public int? QuestionId { get; set; }
        public int OrderNumber { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = "manual";
        public bool IsActual { get; set; } = true;
        public List<AutoQuestionOptionDraftDto> Options { get; set; } = new();
        public int? MaxScore { get; set; }
        public string? ModelAnswer { get; set; }
        public List<QuestionAttachmentDraftDto> Attachments { get; set; } = new();
    }

    public class AutoQuestionOptionDraftDto
    {
        public int TempId { get; set; }
        public int? OptionId { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int SortOrder { get; set; }
    }

    public class QuestionAttachmentDraftDto
    {
        public int TempId { get; set; }
        public int? AttachmentId { get; set; }
        public string? ImageUrl { get; set; }
        public int SortOrder { get; set; }
    }

}
