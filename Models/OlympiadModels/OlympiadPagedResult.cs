
namespace Olimpiadnic.Models.OlympiadModels
{
    public class OlympiadPagedResult
    {
        public IEnumerable<OlympiadCardViewModel> Items { get; set; } = new List<OlympiadCardViewModel>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public OlympiadSearchViewModel? SearchModel { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
