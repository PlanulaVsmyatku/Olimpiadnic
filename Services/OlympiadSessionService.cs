using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Olimpiadnic.Entities;
using Olimpiadnic.Models.OlympiadModels;
namespace Olimpiadnic.Services
{
    public class OlympiadSessionService : IOlympiadSessionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string SessionKeyPrefix = "OlympiadParticipation_";

        public OlympiadSessionService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ISession Session => _httpContextAccessor.HttpContext!.Session;

        public async Task<OlympiadParticipationViewModel> GetOrCreateSessionAsync(
            int olympiadId, int userId, int participantId, List<Question> questions)
        {
            var session = GetSession(olympiadId);

            // Если сессия существует и принадлежит тому же участнику - восстанавливаем
            if (session != null && session.ParticipantId == participantId)
            {
                return session;
            }

            // Пытаемся загрузить из БД (черновики/сохранённые ответы)
            var savedAnswers = await LoadAnswersFromDatabaseAsync(participantId, questions);

            // Создаём новую сессию
            var participation = new OlympiadParticipationViewModel
            {
                OlympiadId = olympiadId,
                OlympiadTitle = questions.FirstOrDefault()?.Olymp?.Title ?? "Олимпиада",
                ParticipantId = participantId,
                CurrentQuestionIndex = savedAnswers.CurrentIndex,
                TotalQuestions = questions.Count,
                IsCompleted = false,
                Questions = savedAnswers.Questions ?? questions.Select((q, index) => new QuestionParticipationViewModel
                {
                    QuestionId = q.QuestId,
                    OrderNumber = q.QuestionOrder, // не null
                    Description = q.Description,
                    Type = q.Type,
                    Attachments = q.QuestionAttachments?.OrderBy(a => a.SortOrder).Select(a => a.ImageUrl).ToList() ?? new List<string>(),
                    Options = new List<AutoQuestionOptionParticipationViewModel>(),
                    SelectedOptionIds = new List<int>(),
                    ManualAnswer = string.Empty
                }).ToList()
            };

            SaveSession(participation);
            return participation;
        }

        public void UpdateAnswer(int olympiadId, int questionIndex, QuestionParticipationViewModel? updatedQuestion)
        {
            var session = GetSession(olympiadId);
            if (session == null) return;

            if (updatedQuestion != null && questionIndex >= 0 && questionIndex < session.Questions.Count)
            {
                session.Questions[questionIndex] = updatedQuestion;
            }
            else if (questionIndex == -1)
            {
                // Обновляем только индекс текущего вопроса
                // (индекс уже изменён в вызывающем коде)
            }

            SaveSession(session);

            // Опционально: сохраняем в БД как черновик (каждый 5-й ответ или по таймеру)
            _ = Task.Run(() => SaveDraftToDatabaseAsync(session));
        }

        public OlympiadParticipationViewModel? GetSession(int olympiadId)
        {
            var json = Session.GetString($"{SessionKeyPrefix}{olympiadId}");
            return json == null ? null : JsonConvert.DeserializeObject<OlympiadParticipationViewModel>(json);
        }

        public void ClearSession(int olympiadId)
        {
            Session.Remove($"{SessionKeyPrefix}{olympiadId}");
        }

        public bool HasSession(int olympiadId)
        {
            return Session.Keys.Contains($"{SessionKeyPrefix}{olympiadId}");
        }

        private void SaveSession(OlympiadParticipationViewModel participation)
        {
            var json = JsonConvert.SerializeObject(participation);
            Session.SetString($"{SessionKeyPrefix}{participation.OlympiadId}", json);
        }

        // TODO: Реализовать загрузку из БД
        private async Task<(int CurrentIndex, List<QuestionParticipationViewModel>? Questions)> LoadAnswersFromDatabaseAsync(
            int participantId, List<Question> questions)
        {
            // Здесь будет загрузка сохранённых ответов из таблицы Submission_items (черновики)
            // Пока возвращаем пустой результат
            return (0, null);
        }

        // TODO: Реализовать сохранение черновика в БД
        private async Task SaveDraftToDatabaseAsync(OlympiadParticipationViewModel participation)
        {
            // Сохранение ответов в БД как черновик (чтобы не потерять при сбое)
            // Можно вызывать каждые N ответов или по таймеру
        }

    }

}
