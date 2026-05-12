using System.ComponentModel.DataAnnotations;

namespace Olimpiadnic.Models.PasswordRecoveryModels
{
    public class ResetPasswordViewModel
    {
        [Required]
        public required string Token { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Введите новый пароль")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть от 6 до 100 символов")]
        [DataType(DataType.Password)]
        [Display(Name = "Новый пароль")]
        //поменять ? на required
        public  string? NewPassword { get; set; }

        [Required(ErrorMessage = "Подтвердите пароль")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Пароли не совпадают")]
        [Display(Name = "Подтверждение пароля")]
        //поменять ? на required
        public string? ConfirmPassword { get; set; }
    }
}

