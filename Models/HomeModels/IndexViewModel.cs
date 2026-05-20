using Olimpiadnic.Models.OlympiadModels;
using System.ComponentModel.DataAnnotations;

namespace Olimpiadnic.Models.HomeModels
{
    public class IndexViewModel
    {
        public OlympiadSearchViewModel? SearchModel { get; set; }
        public List<OlympiadCardViewModel> Olympiads { get; set; } = new();
        public bool HasResults { get; set; }
        public int TotalCount { get; set; }

        // Пагинация
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 12;

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
