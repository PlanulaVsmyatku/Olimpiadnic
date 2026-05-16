using System.ComponentModel.DataAnnotations;

namespace Olimpiadnic.Models.RoleModels
{
    
    public class CompletedOlympiadViewModel
    {
        public int OlympiadId { get; set; }
        public required string Title { get; set; }
        public required DateTime CompletedAt { get; set; }
        public required int TotalScore { get; set; }
        public required int MaxScore { get; set; }
    }

    public class ParticipantResultViewModel
    {
        public int ParticipantId { get; set; }
        public required string ParticipantName { get; set; }
        public required string ParticipantLogin { get; set; }
        public int? Score { get; set; }
        public required string Status { get; set; }
    }

}