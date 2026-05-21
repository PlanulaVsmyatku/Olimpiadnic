using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Olimpiadnic.Data;
using Olimpiadnic.Entities;
using Olimpiadnic.Extensions;
using Olimpiadnic.Models.OlympiadModels;
using Olimpiadnic.Services.Repos;
namespace Olimpiadnic.Services
{
    public class OlympiadSessionService : IOlympiadSessionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOlympiadRepository _repository;
        private const string SessionKeyPrefix = "OlympiadParticipation_";

        public OlympiadSessionService(IHttpContextAccessor httpContextAccessor, IOlympiadRepository repository)
        {
            _httpContextAccessor = httpContextAccessor;
            _repository = repository;
        }

        /// <summary>
        /// Создание новой сессии с вопросами из БД
        /// </summary>
        public async Task<OlympiadParticipationViewModel> CreateSessionAsync(
            int olympiadId,
            int userId,
            int participantId,
            List<Question> questions)
        {
            // Проверяем, не существует ли уже сессия
            var existingSession = GetSession(olympiadId);
            if (existingSession != null)
            {
                // Если сессия существует, возвращаем её
                return existingSession;
            }

            // Создаем новую сессию
            var participation = new OlympiadParticipationViewModel
            {
                OlympiadId = olympiadId,
                OlympiadTitle = (await _repository.GetOlympiadByIdAsync(olympiadId))?.Title ?? "",
                ParticipantId = participantId,
                CurrentQuestionIndex = 0,
                TotalQuestions = questions.Count,
                IsCompleted = false,
                Questions = new List<QuestionParticipationViewModel>()
            };

            // Загружаем каждый вопрос
            foreach (var question in questions.OrderBy(q => q.QuestionOrder))
            {
                var questionVM = new QuestionParticipationViewModel
                {
                    QuestionId = question.QuestId,
                    OrderNumber = question.QuestionOrder,
                    Description = question.Description,
                    Type = question.Type,
                    Attachments = await GetQuestionAttachments(question.QuestId),
                    Options = new List<AutoQuestionOptionParticipationViewModel>(),
                    SelectedOptionIds = new List<int>(),
                    ManualAnswer = string.Empty
                };

                // Для auto-вопросов загружаем варианты ответов
                if (question.Type.StartsWith("auto"))
                {
                    var options = await _repository.GetQuestionWithOptionsAsync(question.QuestId);
                    if (options?.AutoQuestions != null)
                    {
                        questionVM.Options = options.AutoQuestions
                            .OrderBy(o => o.SortOrder)
                            .Select(o => new AutoQuestionOptionParticipationViewModel
                            {
                                OptionId = o.QuestOptionId,
                                OptionText = o.OptionText,
                                IsSelected = false
                            }).ToList();
                    }
                }

                participation.Questions.Add(questionVM);
            }

            // Сохраняем в сессию
            SaveSession(participation);
            return participation;
        }

        /// <summary>
        /// Получение существующей сессии (для восстановления черновика)
        /// </summary>
        public OlympiadParticipationViewModel? GetSession(int olympiadId)
        {
            var key = $"{SessionKeyPrefix}{olympiadId}";
            return _httpContextAccessor.HttpContext?.Session.GetObject<OlympiadParticipationViewModel>(key);
        }

        /// <summary>
        /// Полное обновление сессии
        /// </summary>
        public void UpdateSession(OlympiadParticipationViewModel session)
        {
            if (session == null) return;
            SaveSession(session);
        }

        /// <summary>
        /// Обновление ответа на конкретный вопрос
        /// </summary>
        public void UpdateAnswer(int olympiadId, int questionIndex, QuestionParticipationViewModel answer)
        {
            var session = GetSession(olympiadId);
            if (session == null) return;

            if (questionIndex >= 0 && questionIndex < session.Questions.Count && answer != null)
            {
                // Обновляем ответ на вопрос
                session.Questions[questionIndex] = answer;
                SaveSession(session);
            }
        }

        /// <summary>
        /// Обновление текущего индекса вопроса
        /// </summary>
        public void UpdateCurrentQuestionIndex(int olympiadId, int newIndex)
        {
            var session = GetSession(olympiadId);
            if (session == null) return;

            if (newIndex >= 0 && newIndex < session.TotalQuestions)
            {
                session.CurrentQuestionIndex = newIndex;
                SaveSession(session);
            }
        }

        /// <summary>
        /// Удаление сессии (при завершении олимпиады)
        /// </summary>
        public void DeleteSession(int olympiadId)
        {
            var key = $"{SessionKeyPrefix}{olympiadId}";
            _httpContextAccessor.HttpContext?.Session.Remove(key);
        }

        /// <summary>
        /// Проверка существования сессии
        /// </summary>
        public bool SessionExists(int olympiadId)
        {
            return GetSession(olympiadId) != null;
        }

        /// <summary>
        /// Сохранение сессии
        /// </summary>
        private void SaveSession(OlympiadParticipationViewModel session)
        {
            var key = $"{SessionKeyPrefix}{session.OlympiadId}";
            _httpContextAccessor.HttpContext?.Session.SetObject(key, session);
        }

        /// <summary>
        /// Получение вложений вопроса
        /// </summary>
        private async Task<List<string>> GetQuestionAttachments(int questionId)
        {
            var attachments = await _repository.GetQuestionAttachmentsAsync(questionId);
            return attachments.Select(a => a.ImageUrl).ToList();
        }

        /// <summary>
        /// Получение конкретного вопроса из сессии с сохраненными ответами
        /// </summary>
        public QuestionParticipationViewModel? GetQuestionFromSession(int olympiadId, int questionIndex)
        {
            var session = GetSession(olympiadId);
            if (session == null) return null;

            if (questionIndex < 0 || questionIndex >= session.Questions.Count)
                return null;

            var question = session.Questions[questionIndex];

            // Создаем глубокую копию вопроса, чтобы изменения не влияли на сессию напрямую
            var questionCopy = new QuestionParticipationViewModel
            {
                QuestionId = question.QuestionId,
                OrderNumber = question.OrderNumber,
                Description = question.Description,
                Type = question.Type,
                Attachments = new List<string>(question.Attachments),
                Options = question.Options.Select(o => new AutoQuestionOptionParticipationViewModel
                {
                    OptionId = o.OptionId,
                    OptionText = o.OptionText,
                    IsSelected = o.IsSelected
                }).ToList(),
                SelectedOptionIds = new List<int>(question.SelectedOptionIds ?? new List<int>()),
                ManualAnswer = question.ManualAnswer ?? string.Empty
            };

            return questionCopy;
        }

        /// <summary>
        /// Получение текущего вопроса из сессии
        /// </summary>
        public QuestionParticipationViewModel? GetCurrentQuestion(int olympiadId)
        {
            var session = GetSession(olympiadId);
            if (session == null) return null;

            return GetQuestionFromSession(olympiadId, session.CurrentQuestionIndex);
        }

    }

}

