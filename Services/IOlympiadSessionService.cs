using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Olimpiadnic.Entities;
using Olimpiadnic.Models.OlympiadModels;
namespace Olimpiadnic.Services
{
    public interface IOlympiadSessionService
    {
        /// <summary>
        /// Создание новой сессии с вопросами из БД
        /// </summary>
        Task<OlympiadParticipationViewModel> CreateSessionAsync(
            int olympiadId,
            int participantId,
            List<Question> questions);

        /// <summary>
        /// Получение существующей сессии (восстановление, адресация к конкретной сессии)
        /// </summary>
        OlympiadParticipationViewModel? GetSession(int olympiadId, int participantId);

        /// <summary>
        /// Обновление сессии (сохранение ответов и текущего индекса)
        /// </summary>
        void UpdateSession(OlympiadParticipationViewModel session);

        /// <summary>
        /// Обновление ответа на конкретный вопрос
        /// </summary>
        void UpdateAnswer(OlympiadParticipationViewModel session, int questionIndex, QuestionParticipationViewModel answer);

        /// <summary>
        /// Обновление текущего индекса вопроса
        /// </summary>
        void UpdateCurrentQuestionIndex(OlympiadParticipationViewModel session, int newIndex);

        /// <summary>
        /// Получение конкретного вопроса из сессии с сохраненными ответами
        /// </summary>
        /// <param name="olympiadId">ID олимпиады</param>
        /// <param name="questionIndex">Индекс вопроса (0-based)</param>
        /// <returns>Вопрос с ответами пользователя или null если не найден</returns>
        QuestionParticipationViewModel? GetQuestionFromSession(OlympiadParticipationViewModel session, int questionIndex);

        /// <summary>
        /// Получение текущего вопроса из сессии
        /// </summary>
        QuestionParticipationViewModel? GetCurrentQuestion(OlympiadParticipationViewModel session);

        /// <summary>
        /// Удаление сессии (при завершении олимпиады)
        /// </summary>
        void DeleteSession(OlympiadParticipationViewModel session);

        /// <summary>
        /// Проверка существования сессии
        /// </summary>
        bool SessionExists(int olympiadId, int participantId);
    }
}

