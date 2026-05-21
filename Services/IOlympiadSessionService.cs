using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Olimpiadnic.Entities;
using Olimpiadnic.Models.OlympiadModels;
namespace Olimpiadnic.Services
{
    public interface IOlympiadSessionService
    {
        Task<OlympiadParticipationViewModel> GetOrCreateSessionAsync(
        int olympiadId,
        int userId,
        int participantId,
        List<Question> questions);

        void UpdateAnswer(int olympiadId, int questionIndex, QuestionParticipationViewModel question);

        OlympiadParticipationViewModel? GetSession(int olympiadId);

        void ClearSession(int olympiadId);
    }

}
