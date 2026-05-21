using Olimpiadnic.Entities;
using Olimpiadnic.Models.OlympiadModels;
using Olimpiadnic.Models.RoleModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Olimpiadnic.Services.Repos
{
    public interface IOlympiadRepository
    {
        #region Олимпиады
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
        #endregion

        #region Участники
        Task<IEnumerable<OlympiadParticipant>> GetParticipantsByOlympiadIdAsync(int olympId);
        Task<IEnumerable<OlympiadParticipant>> GetParticipantsByUserAndOlympiadIdsAsync(string userId, List<int> olympiadIds);

        Task<bool> RegisterParticipantAsync(int olympiadId, int userId);
        Task<bool> IsUserRegisteredAsync(int olympiadId, int userId);
        Task UpdateParticipantAsync(OlympiadParticipant participant);
        /// <summary>
        /// Получение или создание участника (если не существует)
        /// </summary>
        /// <param name="olympiadId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<OlympiadParticipant> GetOrCreateParticipantAsync(int olympiadId, int userId);
        #endregion


        #region Вопросы
        /// <summary>
        /// Получение всех вопросов олимпиады (оригиналы, без снапшотов)
        /// </summary>
        /// <param name="olympiadId"></param>
        /// <returns></returns>
        Task<List<Question>> GetQuestionsForParticipationAsync(int olympiadId);

        /// <summary>
        /// Получение вопроса с его вариантами ответов (для auto)
        /// </summary>
        /// <param name="questionId"></param>
        /// <returns></returns>
        Task<Question?> GetQuestionWithOptionsAsync(int questionId);

        /// <summary>
        /// Получение конфигурации ручного вопроса
        /// </summary>
        /// <param name="questionId"></param>
        /// <returns></returns>
        Task<ManualQuestionsConfig?> GetManualQuestionConfigAsync(int questionId);

        /// <summary>
        /// Получение изображений конкретного вопроса
        /// </summary>
        /// <param name="questionId"></param>
        /// <returns></returns>
        Task<List<QuestionAttachment>> GetQuestionAttachmentsAsync(int questionId);
        #endregion

        #region Снапшоты
        /// <summary>
        /// Получение снапшота вопроса по ID оригинала (для конкретной олимпиады)
        /// </summary>
        Task<QuestionsSnapshot?> GetQuestionSnapshotByOriginalIdAsync(int olympiadId, int originalQuestionId);

        /// <summary>
        /// Получение всех снапшотов вопросов олимпиады
        /// </summary>
        Task<List<QuestionsSnapshot>> GetQuestionSnapshotsByOlympiadIdAsync(int olympiadId);

        /// <summary>
        /// Получение вариантов ответов для снапшота (auto-radio/checkbox)
        /// </summary>
        Task<List<AutoQuestionsSnapshot>> GetAutoOptionsSnapshotByQuestionSnapshotIdAsync(int questionSnapshotId);

        /// <summary>
        /// Получение конфигурации ручного вопроса для снапшота
        /// </summary>
        Task<ManualQuestionsConfigSnapshot?> GetManualConfigSnapshotByQuestionSnapshotIdAsync(int questionSnapshotId);
        #region == Ответы ==
        /// <summary>
        /// Сохранение ответа (черновик или финальный)
        /// </summary>
        Task SaveAnswerSubmissionAsync(int participantId, int questionSnapshotId, object answerData);

        /// <summary>
        /// Финальное завершение олимпиады
        /// </summary>
        Task CompleteOlympiadAsync(int participantId, int totalScore);
        #endregion

        /// <summary>
        /// Получает снапшот вопроса по ID снапшота
        /// </summary>
        Task<QuestionsSnapshot?> GetQuestionSnapshotByIdAsync(int questionSnapshotId);
        #endregion

        #region Результаты участников
        /// <summary>
        /// Получает результат участника с его ответами (SubmissionItems)
        /// </summary>
        /// <param name="participantId">ID участника</param>
        /// <returns>Результат участника или null</returns>
        Task<OlympiadResult?> GetParticipantResultAsync(int participantId);

        /// <summary>
        /// Получает результаты участника с ответами и проверкой для отображения
        /// </summary>
        Task<ParticipantResultsViewModel?> GetParticipantResultsForDisplayAsync(int participantId);
        #endregion

    }

}