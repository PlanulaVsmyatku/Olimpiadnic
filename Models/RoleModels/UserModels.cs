using System.ComponentModel.DataAnnotations;

namespace Olimpiadnic.Models.RoleModels
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

}
