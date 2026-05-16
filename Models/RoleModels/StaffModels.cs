namespace Olimpiadnic.Models.RoleModels
{
    public class StaffOlympiadCardViewModel
    {
        public int OlympiadId { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required string ImageUrl { get; set; }
        public required DateTime EventStart { get; set; }
        public required DateTime EventEnd { get; set; }
        public int ParticipantsCount { get; set; }
        public int PendingManualChecks { get; set; }
    }

}
