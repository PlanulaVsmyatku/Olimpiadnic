using System.ComponentModel.DataAnnotations;
namespace Olimpiadnic.Models.AccountModels
{
    public class ParticipantRegisterViewModel : RegisterViewModel
    {
        //=== участник ===
        [Required(ErrorMessage = "Укажите город")]
        [Display(Name = "Город")]
        public required string City { get; set; }

        [Required(ErrorMessage = "Укажите учебное заведение")]
        [Display(Name = "Учебное заведение")]
        public required string EducationalInstitution { get; set; }

        [Required(ErrorMessage = "Выберите уровень образования")]
        [Display(Name = "Уровень образования")]
        public required string EducationLevel { get; set; }

        [Display(Name = "Куратор (ФИО контактного лица)")]
        public required string Curator { get; set; }

        [Required(ErrorMessage = "Загрузите скан согласия на обработку ПД")]
        [Display(Name = "Скан согласия на обработку ПД")]
        public required IFormFile ConsentFile { get; set; }

        // Для отображения имени загруженного файла (опционально)
        public string? ConsentFileName { get; set; }
    }
}
