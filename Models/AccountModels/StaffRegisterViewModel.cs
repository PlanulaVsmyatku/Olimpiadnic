using System.ComponentModel.DataAnnotations;
namespace Olimpiadnic.Models.AccountModels
{

    /// <summary>
    /// Класс для отображения форы регистрации
    /// </summary>
    public class StaffRegisterFormModel
    {
        public required string InviteToken { get; set; }
        public required string Email { get; set; }
    }
    /// <summary>
    /// Класс для записи и валидации формы регистрации сотрудника
    /// Поля: === string InviteToken; 
    ///       string Phone; 
    ///       string Department; === *
    ///        === наследованные: string Login; string Password; string FullName; string Email; ===
    /// </summary>
    public class StaffRegisterViewModel : RegisterViewModel
    {
        //=== Сотрудник ===
        [Required(ErrorMessage = "Необходима ссылка-приглашение")]
        public required string InviteToken { get; set; }

        [Required(ErrorMessage = "Введите номер телефона")]
        [Phone(ErrorMessage = "Некорректный формат телефона")]
        [Display(Name = "Телефон")]
        public required string Phone { get; set; }

        //Выпадающий список ( Отдел дополнительного образования, Методический отдел,  учебная часть - кафедра 1, учебная часть - кафедра 2, учебная часть - кафедра 3)
        [Required(ErrorMessage = "Выберите отдел")]
        [Display(Name = "Отдел")]
        public required string Department { get; set; }
    }
}
