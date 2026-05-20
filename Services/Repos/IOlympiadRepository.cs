using Olimpiadnic.Entities;
using Olimpiadnic.Models.OlympiadModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Olimpiadnic.Services.Repos
{
    public interface IOlympiadRepository
    {
        // Олимпиады
        Task<IEnumerable<Olympiad>> GetAllOlympiadsAsync();
        Task<Olympiad> GetOlympiadByIdAsync(int olympId);
        Task<IEnumerable<Olympiad>> GetActiveOlympiadsAsync();

        // Пагинированный список с фильтрацией
        Task<OlympiadPagedResult> GetActiveOlympiadsPagedFilteredAsync(
            OlympiadSearchViewModel? searchModel,
            int pageNumber,
            int pageSize = 12);

        //Участники
        Task<IEnumerable<OlympiadParticipant>> GetParticipantsByOlympiadIdAsync(int olympId);
        Task<IEnumerable<OlympiadParticipant>> GetParticipantsByUserAndOlympiadIdsAsync(string userId, List<int> olympiadIds);
    }

}