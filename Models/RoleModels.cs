using System.ComponentModel.DataAnnotations;

namespace Olimpiadnic.Models
{
    public static class UserRoles
    {
        public const string Participant = "Участник";
        public const string Staff = "Сотрудник";
        public const string Admin = "Администратор";

        public static readonly List<string> AllRoles = new()
        { Participant, Staff, Admin };
    }

    public class UserProfileViewModel
    {
        [Display(Name = "Логин")]
        public required string Login { get; set; }

        [Display(Name = "Полное имя")]
        public required string FullName { get; set; }

        [Display(Name = "Электронная почта")]
        [EmailAddress]
        public required string Email { get; set; }

        [Display(Name = "Номер телефона")]
        [Phone]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Учебное заведение")]
        public string? EducationalInstitution { get; set; }

        [Display(Name = "Уровень образования")]
        public string? EducationLevel { get; set; }

        [Display(Name = "Должность")]
        public string? Position { get; set; }

        [Display(Name = "Статус аккаунта")]
        public bool IsActive { get; set; }

        [Display(Name = "Роль")]
        public required string Role { get; set; }
    }

    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Введите логин")]
        [Display(Name = "Логин")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Логин должен быть от 3 до 20 символов")]
        public required string Login { get; set; }

        [Required(ErrorMessage = "Введите полное имя")]
        [Display(Name = "Полное имя")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "ФИО должно содержать от 5 до 200 символов")]
        public required string FullName { get; set; }

        [Required(ErrorMessage = "Введите email")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        [Display(Name = "Электронная почта")]
        public required string Email { get; set; }

        [Display(Name = "Номер телефона")]
        [Phone(ErrorMessage = "Некорректный формат телефона")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Учебное заведение")]
        public string? EducationalInstitution { get; set; }

        [Display(Name = "Уровень образования")]
        public string? EducationLevel { get; set; }

        [Display(Name = "Должность")]
        public string? Position { get; set; }
    }

    public class CompletedOlympiadViewModel
    {
        public int OlympiadId { get; set; }
        public required string Title { get; set; }
        public required DateTime CompletedAt { get; set; }
        public required int TotalScore { get; set; }
        public required int MaxScore { get; set; }
    }

    public class ParticipantResultViewModel
    {
        public int ParticipantId { get; set; }
        public required string ParticipantName { get; set; }
        public required string ParticipantLogin { get; set; }
        public int? Score { get; set; }
        public required string Status { get; set; }
    }

    public class StaffOlympiadCardViewModel
    {
        public int OlympiadId { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required string ImageUrl { get; set; }
        public required DateTime EventStart { get; set; }
        public required DateTime EventEnd { get; set; }
        public int ParticipantsCount { get; set; }
        public int PendingManualChecks { get; set; }
    }

    // Модель для администратора - статистика
    public class AdminStatisticsViewModel
    {
        [Display(Name = "Всего пользователей")]
        public int TotalUsers { get; set; }

        [Display(Name = "Всего олимпиад")]
        public int TotalOlympiads { get; set; }

        [Display(Name = "Всего участников (записей)")]
        public int TotalParticipations { get; set; }

        [Display(Name = "Активных олимпиад")]
        public int ActiveOlympiads { get; set; }

        [Display(Name = "Завершённых олимпиад")]
        public int CompletedOlympiads { get; set; }

        public List<OlympiadStatisticsViewModel> OlympiadStats { get; set; } = new();
    }

    public class OlympiadStatisticsViewModel
    {
        public int OlympiadId { get; set; }
        public required string Title { get; set; }
        public int ParticipantsCount { get; set; }
        public int CompletedCount { get; set; }
        public required string Status { get; set; }
    }

    // Модель для администратора - пользователи
    public class UserAdminViewModel
    {
        public int UserId { get; set; }
        public required string Login { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime RegisteredAt { get; set; }
    }

    // Модель для редактирования пользователя администратором
    public class EditUserByAdminViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Введите логин")]
        [Display(Name = "Логин")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Логин должен быть от 3 до 20 символов")]
        public string? Login { get; set; }

        [Required(ErrorMessage = "Введите полное имя")]
        [Display(Name = "Полное имя")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "ФИО должно содержать от 5 до 200 символов")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Введите email")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        [Display(Name = "Электронная почта")]
        public string? Email { get; set; }

        [Display(Name = "Номер телефона")]
        [Phone(ErrorMessage = "Некорректный формат телефона")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Учебное заведение")]
        public string? EducationalInstitution { get; set; }

        [Display(Name = "Уровень образования")]
        public string? EducationLevel { get; set; }

        [Display(Name = "Должность")]
        public string? Position { get; set; }

        [Required(ErrorMessage = "Выберите роль")]
        [Display(Name = "Роль")]
        public string? Role { get; set; }

        [Display(Name = "Активен")]
        public bool IsActive { get; set; }

        public List<string> AvailableRoles { get; set; } = new();
    }
}