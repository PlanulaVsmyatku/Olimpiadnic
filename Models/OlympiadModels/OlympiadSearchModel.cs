using System.ComponentModel.DataAnnotations;

namespace Olimpiadnic.Models.OlympiadModels
{
    // Модель для поиска
    public class OlympiadSearchViewModel
    {
        [Display(Name = "Название олимпиады")]
        public string? SearchTitle { get; set; }

        [Display(Name = "Дата начала (от)")]
        [DataType(DataType.Date)]
        public DateTime? StartDateFrom { get; set; }

        [Display(Name = "Дата начала (до)")]
        [DataType(DataType.Date)]
        public DateTime? StartDateTo { get; set; }

        [Display(Name = "Дата окончания (от)")]
        [DataType(DataType.Date)]
        public DateTime? EndDateFrom { get; set; }

        [Display(Name = "Дата окончания (до)")]
        [DataType(DataType.Date)]
        public DateTime? EndDateTo { get; set; }
    }
}
