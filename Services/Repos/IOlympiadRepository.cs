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
        /// <summary>
        /// Возвращает лист типа Entities.Olympiad со всеми олимпиадами
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Olympiad>> GetAllOlympiadsAsync();
        /// <summary>
        /// Возвращает конкретную олимпиаду по ID
        /// </summary>
        /// <param name="olympId">ID конкретной олимпиады</param>
        /// <returns></returns>
        Task<Olympiad> GetOlympiadByIdAsync(int olympId);
        /// <summary>
        /// Возвращает только олимпиады, которые ещё не начались или в процессе
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Olympiad>> GetActiveOlympiadsAsync();

        // Пагинированный список с фильтрацией
        /// <summary>
        /// Возвращает лист активных олимпиад (не завершившихся), раздёленных на группы по <= pageSize если их больше указанного pageSize, учитывая поисковые фильтры
        /// </summary>
        /// <param name="searchModel">Модель связанная с поисковыми полями в представлении</param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize">Как много элементов будет на одной странице</param>
        /// <returns></returns>
        Task<OlympiadPagedResult> GetActiveOlympiadsPagedFilteredAsync(
            OlympiadSearchViewModel? searchModel,
            int pageNumber,
            int pageSize = 12);

        /// <summary>
        /// Возвращает расширенную информацию об олимпиаде
        /// </summary>
        /// <param name="olympId">ID олимпиады с которой нужны детали</param>
        /// <returns></returns>
        Task<Olympiad?> GetOlympiadWithDetailsAsync(int olympId);
        /// <summary>
        /// Возвращает общее число заданий кокнретной олимпиады
        /// </summary>
        /// <param name="olympId">ID олимпиады с которой нужно число заданий</param>
        /// <returns></returns>
        Task<int> GetTotalQuestionsCountAsync(int olympId);

        //Участники
        Task<IEnumerable<OlympiadParticipant>> GetParticipantsByOlympiadIdAsync(int olympId);
        Task<IEnumerable<OlympiadParticipant>> GetParticipantsByUserAndOlympiadIdsAsync(string userId, List<int> olympiadIds);
    }

}