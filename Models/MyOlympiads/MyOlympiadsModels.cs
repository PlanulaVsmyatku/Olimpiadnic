using System.ComponentModel.DataAnnotations;

namespace Olimpiadnic.Models.MyOlympiads
{
    /// <summary>
    /// Базовый класс для отображения олимпиады
    /// </summary>
    public class MyOlympiadBase
    {
        public int OlympiadId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventStart { get; set; }
        public DateTime EventEnd { get; set; }
        public DateTime RegistOpen { get; set; }
        public DateTime RegistClosed { get; set; }

        public string Status
        {
            get
            {
                var now = DateTime.Now;
                if (now >= EventStart && now <= EventEnd)
                    return "in_progress";
                if (now > EventEnd)
                    return "ended";
                return "available";
            }
        }

        public string StatusText => Status switch
        {
            "in_progress" => "Идёт",
            "ended" => "Завершена",
            "available" => "Скоро",
            _ => "Неизвестно"
        };

        public string StatusColor => Status switch
        {
            "in_progress" => "#3b82f6",
            "ended" => "#ef4444",
            "available" => "#10b981",
            _ => "#6b7280"
        };
    }

    /// <summary>
    /// Модель для участника - олимпиады, на которые записан
    /// </summary>
    public class ParticipantOlympiadViewModel : MyOlympiadBase
    {
        public int? UserTotalScore { get; set; }
        public int MaxPossibleScore { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsRegistered { get; set; }
        public DateTime? CompletedAt { get; set; }

        public int? Percentage => MaxPossibleScore > 0 && UserTotalScore.HasValue
            ? (UserTotalScore.Value * 100 / MaxPossibleScore)
            : null;

        public string ScoreDisplay => IsCompleted && UserTotalScore.HasValue
            ? $"{UserTotalScore}/{MaxPossibleScore}"
            : "Не завершена";
    }

    /// <summary>
    /// Модель для сотрудника - олимпиады, где он автор или проверяющий
    /// </summary>
    public class StaffOlympiadViewModel : MyOlympiadBase
    {
        public bool IsAuthor { get; set; }
        public bool IsReviewer { get; set; }
        public int? UncheckedManualAnswers { get; set; }
        public int? TotalParticipants { get; set; }
        public bool HasUncheckedAnswers => UncheckedManualAnswers > 0;
        public bool HasParticipants => TotalParticipants > 0;

        public string RoleText => IsAuthor && IsReviewer ? "Автор и проверяющий"
                                    : IsAuthor ? "Автор"
                                    : IsReviewer ? "Проверяющий"
                                    : "Участник команды";
    }

    /// <summary>
    /// Модель для администратора - все олимпиады (табличный вид)
    /// </summary>
    public class AdminOlympiadViewModel
    {
        public int OlympiadId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Credentials { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime EventStart { get; set; }
        public DateTime EventEnd { get; set; }
        public DateTime RegistOpen { get; set; }
        public DateTime RegistClosed { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Модель для фильтрации
    /// </summary>
    public class MyOlympiadsFilterViewModel
    {
        [Display(Name = "Название")]
        public string? SearchTitle { get; set; }

        [Display(Name = "Дата начала (от)")]
        [DataType(DataType.Date)]
        public DateTime? StartDateFrom { get; set; }

        [Display(Name = "Дата начала (до)")]
        [DataType(DataType.Date)]
        public DateTime? StartDateTo { get; set; }

        [Display(Name = "Дата окончания (от)")]
        [DataType(DataType.Date)]
        public DateTime? EndDateFrom { get; set; }

        [Display(Name = "Дата окончания (до)")]
        [DataType(DataType.Date)]
        public DateTime? EndDateTo { get; set; }

        [Display(Name = "Только завершённые")]
        public bool OnlyCompleted { get; set; }

        [Display(Name = "Только с записью (без результатов)")]
        public bool OnlyRegistered { get; set; }
    }

    /// <summary>
    /// Основная модель для страницы "Мои олимпиады"
    /// </summary>
    public class MyOlympiadsViewModel
    {
        public MyOlympiadsFilterViewModel Filter { get; set; } = new();

        // Для участника
        public List<ParticipantOlympiadViewModel> ParticipantOlympiads { get; set; } = new();

        // Для сотрудника
        public List<StaffOlympiadViewModel> StaffOlympiads { get; set; } = new();

        // Для администратора
        public List<AdminOlympiadViewModel> AdminOlympiads { get; set; } = new();

        // Пагинация
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 6;

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
