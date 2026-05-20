using System.ComponentModel.DataAnnotations;

namespace Olimpiadnic.Models.RoleModels
{
    public static class UserRoles
    {
        public const string participant = "Участник";
        public const string staff = "Сотрудник";
        public const string admin = "Администратор";

        public static readonly List<string> AllRoles = new()
        { participant, staff, admin };
    }

    public class UserProfileViewModel
    {
        [Display(Name = "Логин")]
        public required string Login { get; set; }

        [Display(Name = "Полное имя")]
        public required string FullName { get; set; }

        [Display(Name = "Роль")]
        public required string Role { get; set; }

        [Display(Name = "Электронная почта")]
        [EmailAddress]
        public required string Email { get; set; }
    }

    public class ParticipantProfileViewModel : UserProfileViewModel
    {
        

        [Display(Name = "Уровень образования")]
        public required string EducationLevel { get; set; }

        [Display(Name = "Город")]
        public required string City { get; set; }

        [Display(Name = "Учебное заведение")]
        public required string EducationalInstitution { get; set; }

        [Display(Name = "Куратор")]
        public required string Curator { get; set; }

        [Display(Name = "Статус аккаунта")]
        public required bool IsActive { get; set; }

    }

    public class StaffProfileViewModel : UserProfileViewModel
    {

        [Display(Name = "Номер телефона")]
        [Phone]
        public required string PhoneNumber { get; set; }

        [Display(Name = "Учебное заведение")]
        public required string EducationalInstitution { get; set; }

        [Display(Name = "Отдел")]
        public required string Departament { get; set; }

        [Display(Name = "Город")]
        public required string City { get; set; }
    }

    public class AdminProfileViewModel : UserProfileViewModel
    {

    }

}
