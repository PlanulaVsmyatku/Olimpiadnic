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

        public async Task<OlympiadParticipationViewModel> GetOrCreateSessionAsync(
            int olympiadId,
            int userId,
            int participantId,
            List<Question> questions)
        {
            var session = GetSession(olympiadId);
            if (session != null)
                return session;

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

            SaveSession(participation);
            return participation;
        }

        public void UpdateAnswer(int olympiadId, int questionIndex, QuestionParticipationViewModel? question)
        {
            var session = GetSession(olympiadId);
            if (session == null) return;

            if (questionIndex >= 0 && questionIndex < session.Questions.Count && question != null)
            {
                session.Questions[questionIndex] = question;
            }

            SaveSession(session);
        }

        public OlympiadParticipationViewModel? GetSession(int olympiadId)
        {
            var key = $"{SessionKeyPrefix}{olympiadId}";
            return _httpContextAccessor.HttpContext?.Session.GetObject<OlympiadParticipationViewModel>(key);
        }

        public void ClearSession(int olympiadId)
        {
            var key = $"{SessionKeyPrefix}{olympiadId}";
            _httpContextAccessor.HttpContext?.Session.Remove(key);
        }

        private void SaveSession(OlympiadParticipationViewModel session)
        {
            var key = $"{SessionKeyPrefix}{session.OlympiadId}";
            _httpContextAccessor.HttpContext?.Session.SetObject(key, session);
        }

        private async Task<List<string>> GetQuestionAttachments(int questionId)
        {
            // Получить вложения из БД
            var attachments = await _repository.GetQuestionAttachmentsAsync(questionId);
            return attachments.Select(a => a.ImageUrl).ToList();
        }

    }
}
