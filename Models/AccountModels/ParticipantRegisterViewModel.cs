using System.ComponentModel.DataAnnotations;

namespace Olimpiadnic.Models.AccountModels
{
    /// <summary>
    /// Данные характерные для участника
    /// </summary>
    public class ParticipantRegisterViewModel : ProfileModel
    {

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

