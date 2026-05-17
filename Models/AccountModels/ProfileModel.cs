using System.ComponentModel.DataAnnotations;
namespace Olimpiadnic.Models.AccountModels
{
    /// <summary>
    /// Общие данные учётных записей участника и сотрудника
    /// </summary>
    public class ProfileModel
    {
        [Required(ErrorMessage = "Введите логин")]
        [Display(Name = "Логин")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Логин должен быть от 3 до 20 символов")]
        [RegularExpression(@"^[a-zA-Zа-яА-Я0-9_]+$", ErrorMessage = "Логин может содержать только буквы (латиница или кириллица), цифры и знак подчеркивания")]
        public required string Login { get; set; }

        [Required(ErrorMessage = "Введите пароль")]
        [Display(Name = "Пароль")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть от 6 до 100 символов")]
        public required string Password { get; set; }

        //в БД не идёт. Только для отображения ошибки в представлении
        [Required(ErrorMessage = "Подтвердите пароль")]
        [Display(Name = "Подтверждение пароля")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public required string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Укажите ФИО")]
        [Display(Name = "ФИО")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "ФИО должно содержать от 5 до 200 символов")]
        public required string FullName { get; set; }

        [Required(ErrorMessage = "Введите email")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        [Display(Name = "Email")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Укажите город")]
        [Display(Name = "Город")]
        public required string City { get; set; }

        [Required(ErrorMessage = "Укажите учебное заведение")]
        [Display(Name = "Учебное заведение")]
        public required string EducationalInstitution { get; set; }



    }
}
