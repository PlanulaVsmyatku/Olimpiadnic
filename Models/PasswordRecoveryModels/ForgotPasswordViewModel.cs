using System.ComponentModel.DataAnnotations;

namespace Olimpiadnic.Models.PasswordRecoveryModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Введите email")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        [Display(Name = "Email")]
        public required string Email { get; set; }
    }
}
