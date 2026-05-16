using System.ComponentModel.DataAnnotations;

namespace Olimpiadnic.Models.OlympiadModels
{
    //Создание олимпиады
    public class CreateOlympiadViewModel
    {
        public int OlympiadId { get; set; }

        [Required(ErrorMessage = "Введите название олимпиады")]
        [Display(Name = "Название олимпиады")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Название должно быть от 3 до 200 символов")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Изображение олимпиады")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Загрузить изображение")]
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
        public DateTime RegistOpen { get; set; } = DateTime.Now.AddDays(7);

        [Required(ErrorMessage = "Укажите дату окончания регистрации")]
        [Display(Name = "Окончание регистрации")]
        [DataType(DataType.DateTime)]
        public DateTime RegistClosed { get; set; } = DateTime.Now.AddDays(30);

        [Required(ErrorMessage = "Укажите дату начала олимпиады")]
        [Display(Name = "Начало олимпиады")]
        [DataType(DataType.DateTime)]
        public DateTime EventStart { get; set; } = DateTime.Now.AddDays(31);

        [Required(ErrorMessage = "Укажите дату окончания олимпиады")]
        [Display(Name = "Окончание олимпиады")]
        [DataType(DataType.DateTime)]
        public DateTime EventEnd { get; set; } = DateTime.Now.AddDays(38);

        [Display(Name = "Статус")]
        public string Status { get; set; } = "available";

        public List<QuestionViewModel> Questions { get; set; } = new();
    }
    //Создание вопроса
    public class QuestionViewModel
    {
        public int QuestionId { get; set; }
        public int TempId { get; set; } // Временный ID для нового вопроса

        [Range(minimum: 1, maximum: 100, ErrorMessage = "Номер вопроса должен быть от 0 до 100")]
        [Display(Name = "Номер вопроса")]
        public int OrderNumber { get; set; }

        [Required(ErrorMessage = "Введите описание вопроса")]
        [Display(Name = "Описание вопроса")]
        [StringLength(2000, MinimumLength = 5, ErrorMessage = "Описание должно быть от 5 до 2000 символов")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Выберите тип вопроса")]
        [Display(Name = "Тип вопроса")]
        public string Type { get; set; } = "auto"; // auto или manual

        public bool IsActual { get; set; } = true;

        // Для авто-вопросов
        public List<AutoQuestionOptionViewModel> Options { get; set; } = new();

        // Для ручных вопросов
        [Display(Name = "Максимальный балл")]
        [Range(1, 100, ErrorMessage = "Максимальный балл должен быть от 1 до 100")]
        public int? MaxScore { get; set; }

        [Display(Name = "Эталонный ответ")]
        [StringLength(5000, ErrorMessage = "Эталон не должен превышать 5000 символов")]
        public string? ModelAnswer { get; set; }

        // Вложения
        public List<QuestionAttachmentViewModel> Attachments { get; set; } = new();
    }

    public class AutoQuestionOptionViewModel
    {
        public int OptionId { get; set; }
        public int TempId { get; set; }

        [Required(ErrorMessage = "Введите текст варианта ответа")]
        [Display(Name = "Текст варианта")]
        public string OptionText { get; set; } = string.Empty;

        [Display(Name = "Правильный ответ")]
        public bool IsCorrect { get; set; }

        [Display(Name = "Порядок")]
        public int SortOrder { get; set; }
    }

    public class QuestionAttachmentViewModel
    {
        public int AttachmentId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public IFormFile? ImageFile { get; set; }
        public int SortOrder { get; set; }
    }
}
