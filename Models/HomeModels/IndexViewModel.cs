using Olimpiadnic.Models.OlympiadModels;
using System.ComponentModel.DataAnnotations;

namespace Olimpiadnic.Models.HomeModels
{
    public class IndexViewModel
    {
        public OlympiadSearchViewModel? SearchModel { get; set; }
        public List<OlympiadCardViewModel> Olympiads { get; set; } = new();
        public bool HasResults { get; set; }

        [Display(Name = "Всего найдено")]
        public int TotalCount { get; set; }
    }
}
