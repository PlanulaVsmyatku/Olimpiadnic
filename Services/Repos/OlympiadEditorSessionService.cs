using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Olimpiadnic.Extensions;
using Olimpiadnic.Models.RoleModels;

namespace Olimpiadnic.Services.Repos
{
    public class OlympiadEditorSessionService : IOlympiadEditorSessionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string SessionKeyPrefix = "OlympiadEditor_";

        public OlympiadEditorSessionService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetSessionKey(int userId) => $"{SessionKeyPrefix}{userId}";

        private ISession Session => _httpContextAccessor.HttpContext?.Session
            ?? throw new InvalidOperationException("Session not available");

        public Task<CreateOlympiadViewModel?> GetSessionAsync(int userId)
        {
            var key = GetSessionKey(userId);
            var session = Session.GetObject<CreateOlympiadViewModel>(key);
            return Task.FromResult(session);
        }

        public Task SaveSessionAsync(int userId, CreateOlympiadViewModel model)
        {
            var key = GetSessionKey(userId);
            Session.SetObject(key, model);
            return Task.CompletedTask;
        }

        public Task DeleteSessionAsync(int userId)
        {
            var key = GetSessionKey(userId);
            Session.Remove(key);
            return Task.CompletedTask;
        }

        public bool SessionExists(int userId)
        {
            var key = GetSessionKey(userId);
            return Session.ContainsKey(key);
        }

        public async Task<QuestionEditorViewModel?> GetQuestionAsync(int userId, int questionIndex)
        {
            var session = await GetSessionAsync(userId);
            if (session == null || questionIndex < 0 || questionIndex >= session.Questions.Count)
                return null;

            return session.Questions[questionIndex];
        }

        public async Task UpdateQuestionAsync(int userId, int questionIndex, QuestionEditorViewModel question)
        {
            var session = await GetSessionAsync(userId);
            if (session == null) return;

            if (questionIndex >= 0 && questionIndex < session.Questions.Count)
            {
                // Сохраняем оригинальный TempId и QuestionId
                question.TempId = session.Questions[questionIndex].TempId;
                question.QuestionId = session.Questions[questionIndex].QuestionId;
                question.OrderNumber = questionIndex + 1;

                session.Questions[questionIndex] = question;
                await SaveSessionAsync(userId, session);
            }
        }

        public async Task<int> AddQuestionAsync(int userId)
        {
            var session = await GetSessionAsync(userId);
            if (session == null)
            {
                // Создаём новую сессию с дефолтными значениями
                session = GetDefaultModel();
                await SaveSessionAsync(userId, session);
            }

            var newId = session.Questions.Any() ? session.Questions.Max(q => q.TempId) + 1 : 1;

            var newQuestion = new QuestionEditorViewModel
            {
                TempId = newId,
                OrderNumber = session.Questions.Count + 1,
                Type = "manual",
                IsExpanded = true,
                Description = string.Empty,
                Options = new List<AutoQuestionOptionEditorViewModel>(),
                Attachments = new List<QuestionAttachmentEditorViewModel>(),
                MaxScore = 10,
                ModelAnswer = string.Empty
            };

            session.Questions.Add(newQuestion);
            await SaveSessionAsync(userId, session);

            return newQuestion.TempId;
        }

        public async Task RemoveQuestionAsync(int userId, int questionIndex)
        {
            var session = await GetSessionAsync(userId);
            if (session == null || questionIndex < 0 || questionIndex >= session.Questions.Count)
                return;

            session.Questions.RemoveAt(questionIndex);

            // Перенумеровываем
            for (int i = 0; i < session.Questions.Count; i++)
            {
                session.Questions[i].OrderNumber = i + 1;
            }

            // Корректируем текущий индекс
            if (session.CurrentQuestionIndex >= session.Questions.Count)
            {
                session.CurrentQuestionIndex = Math.Max(0, session.Questions.Count - 1);
            }

            await SaveSessionAsync(userId, session);
        }

        public async Task ReorderQuestionsAsync(int userId, List<int> newOrder)
        {
            var session = await GetSessionAsync(userId);
            if (session == null) return;

            var reorderedQuestions = newOrder
                .Select((tempId, index) =>
                {
                    var question = session.Questions.FirstOrDefault(q => q.TempId == tempId);
                    if (question != null)
                    {
                        question.OrderNumber = index + 1;
                    }
                    return question;
                })
                .Where(q => q != null)
                .ToList();

            session.Questions = reorderedQuestions!;
            await SaveSessionAsync(userId, session);
        }

        private CreateOlympiadViewModel GetDefaultModel()
        {
            return new CreateOlympiadViewModel
            {
                OlympiadId = 0,
                IsEditMode = false,
                Title = string.Empty,
                Description = string.Empty,
                Credentials = string.Empty,
                ImageUrl = string.Empty,
                RegistOpen = DateTime.Now.AddDays(7),
                RegistClosed = DateTime.Now.AddDays(30),
                EventStart = DateTime.Now.AddDays(31),
                EventEnd = DateTime.Now.AddDays(38),
                CurrentQuestionIndex = 0,
                Questions = new List<QuestionEditorViewModel>
                {
                    new QuestionEditorViewModel
                    {
                        TempId = 1,
                        OrderNumber = 1,
                        Type = "manual",
                        IsExpanded = true,
                        Description = string.Empty,
                        MaxScore = 10,
                        ModelAnswer = string.Empty,
                        Options = new List<AutoQuestionOptionEditorViewModel>(),
                        Attachments = new List<QuestionAttachmentEditorViewModel>()
                    }
                }
            };
        }
    }

}
