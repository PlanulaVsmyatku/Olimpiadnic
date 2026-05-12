using System.ComponentModel.DataAnnotations;

namespace Olimpiadnic.Models.AccountModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Введите логин или email")]
        [Display(Name = "Логин или Email")]
        public required string LoginOrEmail { get; set; }

        [Required(ErrorMessage = "Введите пароль")]
        [Display(Name = "Пароль")]
        [DataType(DataType.Password)]
        public required string Password { get; set; }

        [Display(Name = "Запомнить меня")]
        public bool RememberMe { get; set; }
    }
}
