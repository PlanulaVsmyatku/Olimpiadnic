using System.ComponentModel.DataAnnotations;

namespace Olimpiadnic.Models.OlympiadModels
{
    public class OlympiadCardViewModel
    {
        public int OlympiadId { get; set; }

        [Display(Name = "Название")]
        public required string Title { get; set; }

        [Display(Name = "Описание")]
        public required string Description { get; set; }

        [Display(Name = "Изображение")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Начало проведения")]
        [DataType(DataType.DateTime)]
        public required DateTime EventStart { get; set; }

        [Display(Name = "Окончание проведения")]
        [DataType(DataType.DateTime)]
        public required DateTime EventEnd { get; set; }

        [Display(Name = "Открытие регистрации")]
        [DataType(DataType.DateTime)]
        public required DateTime RegistOpen { get; set; }

        [Display(Name = "Закрытие регистрации")]
        [DataType(DataType.DateTime)]
        public required DateTime RegistClosed { get; set; }

        [Display(Name = "Статус")]
        public required string Status { get; set; }

        [Display(Name = "Пользователь уже записан")]
        public bool IsUserRegistered { get; set; } = false;

    }

}
