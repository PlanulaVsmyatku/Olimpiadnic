using Microsoft.AspNetCore.Http;
using Olimpiadnic.Extensions;
using Olimpiadnic.Models.RoleModels;
using System.Text.Json;

namespace Olimpiadnic.Services.Session
{
    public class OlympiadEditorSessionService : IOlympiadEditorSessionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string SessionKeyPrefix = "OlympiadEditor_";
        private static int _nextTempId = 1000;

        public OlympiadEditorSessionService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetSessionKey(int userId) => $"{SessionKeyPrefix}{userId}";

        public async Task<CreateOlympiadViewModel?> GetSessionAsync(int userId)
        {
            var key = GetSessionKey(userId);
            var session = _httpContextAccessor.HttpContext?.Session;

            if (session == null) return null;

            var model = session.GetObject<CreateOlympiadViewModel>(key);

            if (model != null && model.Questions == null)
            {
                model.Questions = new List<QuestionEditorViewModel>();
            }

            return await Task.FromResult(model);
        }

        public async Task SaveSessionAsync(int userId, CreateOlympiadViewModel model)
        {
            var key = GetSessionKey(userId);
            _httpContextAccessor.HttpContext?.Session.SetObject(key, model);
            await Task.CompletedTask;
        }

        public async Task DeleteSessionAsync(int userId)
        {
            var key = GetSessionKey(userId);
            _httpContextAccessor.HttpContext?.Session.Remove(key);
            await Task.CompletedTask;
        }

        public bool SessionExists(int userId)
        {
            var key = GetSessionKey(userId);
            return _httpContextAccessor.HttpContext?.Session.ContainsKey(key) ?? false;
        }

        // Обновление основной информации
        public async Task UpdateOlympiadInfoAsync(int userId, CreateOlympiadViewModel updatedInfo)
        {
            var session = await GetSessionAsync(userId);
            if (session == null)
            {
                session = new CreateOlympiadViewModel
                {
                    Title = updatedInfo.Title,
                    Description = updatedInfo.Description,
                    Credentials = updatedInfo.Credentials,
                    ImageUrl = updatedInfo.ImageUrl,
                    RegistOpen = updatedInfo.RegistOpen,
                    RegistClosed = updatedInfo.RegistClosed,
                    EventStart = updatedInfo.EventStart,
                    EventEnd = updatedInfo.EventEnd,
                    Questions = new List<QuestionEditorViewModel>()
                };
            }
            else
            {
                session.Title = updatedInfo.Title;
                session.Description = updatedInfo.Description;
                session.Credentials = updatedInfo.Credentials;
                session.ImageUrl = updatedInfo.ImageUrl;
                session.RegistOpen = updatedInfo.RegistOpen;
                session.RegistClosed = updatedInfo.RegistClosed;
                session.EventStart = updatedInfo.EventStart;
                session.EventEnd = updatedInfo.EventEnd;
            }

            await SaveSessionAsync(userId, session);
        }

        // Работа с вопросами
        public async Task<QuestionEditorViewModel?> GetQuestionAsync(int userId, int questionIndex)
        {
            var session = await GetSessionAsync(userId);
            if (session == null || questionIndex < 0 || questionIndex >= session.Questions.Count)
                return null;

            return CloneQuestion(session.Questions[questionIndex]);
        }

        public async Task UpdateQuestionAsync(int userId, int questionIndex, QuestionEditorViewModel question)
        {
            var session = await GetSessionAsync(userId);
            if (session == null || questionIndex < 0 || questionIndex >= session.Questions.Count)
                return;

            session.Questions[questionIndex] = question;
            await SaveSessionAsync(userId, session);
        }

        public async Task<int> AddQuestionAsync(int userId)
        {
            var session = await GetSessionAsync(userId);
            if (session == null)
            {
                session = new CreateOlympiadViewModel
                {
                    OlympiadId = 0,
                    IsEditMode = false,
                    Title = string.Empty,
                    Description = string.Empty,
                    RegistOpen = DateTime.Now.AddDays(7),
                    RegistClosed = DateTime.Now.AddDays(14),
                    EventStart = DateTime.Now.AddDays(21),
                    EventEnd = DateTime.Now.AddDays(28),
                    Questions = new List<QuestionEditorViewModel>()
                };
            }

            var newQuestion = new QuestionEditorViewModel
            {
                TempId = GetNextTempId(),
                QuestionId = null,
                OrderNumber = session.Questions.Count + 1,
                Description = string.Empty,
                Type = "manual",
                IsActual = true,
                IsExpanded = true,
                Options = new List<AutoQuestionOptionEditorViewModel>(),
                Attachments = new List<QuestionAttachmentEditorViewModel>(),
                MaxScore = 10,
                ModelAnswer = string.Empty
            };

            session.Questions.Add(newQuestion);
            await SaveSessionAsync(userId, session);
            return session.Questions.Count - 1;
        }

        public async Task RemoveQuestionAsync(int userId, int questionIndex)
        {
            var session = await GetSessionAsync(userId);
            if (session == null || questionIndex < 0 || questionIndex >= session.Questions.Count)
                return;

            session.Questions.RemoveAt(questionIndex);

            for (int i = 0; i < session.Questions.Count; i++)
            {
                session.Questions[i].OrderNumber = i + 1;
            }

            if (session.CurrentQuestionIndex >= session.Questions.Count)
                session.CurrentQuestionIndex = Math.Max(0, session.Questions.Count - 1);

            await SaveSessionAsync(userId, session);
        }

        public async Task ReorderQuestionsAsync(int userId, List<int> newOrder)
        {
            var session = await GetSessionAsync(userId);
            if (session == null) return;

            var reorderedQuestions = new List<QuestionEditorViewModel>();
            foreach (var index in newOrder)
            {
                if (index >= 0 && index < session.Questions.Count)
                {
                    var question = session.Questions[index];
                    question.OrderNumber = reorderedQuestions.Count + 1;
                    reorderedQuestions.Add(question);
                }
            }

            session.Questions = reorderedQuestions;
            await SaveSessionAsync(userId, session);
        }

        // Работа с опциями вопроса
        public async Task<List<AutoQuestionOptionEditorViewModel>> GetQuestionOptionsAsync(int userId, int questionIndex)
        {
            var question = await GetQuestionAsync(userId, questionIndex);
            return question?.Options ?? new List<AutoQuestionOptionEditorViewModel>();
        }

        public async Task AddQuestionOptionAsync(int userId, int questionIndex, QuestionEditorViewModel question)
        {
            await UpdateQuestionAsync(userId, questionIndex, question);
        }

        public async Task RemoveQuestionOptionAsync(int userId, int questionIndex, int optionIndex)
        {
            var question = await GetQuestionAsync(userId, questionIndex);
            if (question == null || optionIndex < 0 || optionIndex >= question.Options.Count)
                return;

            question.Options.RemoveAt(optionIndex);

            for (int i = 0; i < question.Options.Count; i++)
            {
                question.Options[i].SortOrder = i + 1;
            }

            await UpdateQuestionAsync(userId, questionIndex, question);
        }

        public async Task ReorderQuestionOptionsAsync(int userId, int questionIndex, List<AutoQuestionOptionEditorViewModel> optionsList)
        {
            var question = await GetQuestionAsync(userId, questionIndex);
            if (question == null) return;

            for (int i = 0; i < optionsList.Count; i++)
            {
                optionsList[i].SortOrder = i + 1;
            }

            question.Options = optionsList;
            await UpdateQuestionAsync(userId, questionIndex, question);
        }

        // Работа с вложениями
        public async Task AddAttachmentAsync(int userId, int questionIndex, string imageUrl)
        {
            var question = await GetQuestionAsync(userId, questionIndex);
            if (question == null) return;

            var newAttachment = new QuestionAttachmentEditorViewModel
            {
                TempId = GetNextTempId(),
                AttachmentId = null,
                ImageUrl = imageUrl,
                SortOrder = question.Attachments.Count + 1
            };

            question.Attachments.Add(newAttachment);
            await UpdateQuestionAsync(userId, questionIndex, question);
        }

        public async Task RemoveAttachmentAsync(int userId, int questionIndex, int attachmentIndex)
        {
            var question = await GetQuestionAsync(userId, questionIndex);
            if (question == null || attachmentIndex < 0 || attachmentIndex >= question.Attachments.Count)
                return;

            question.Attachments.RemoveAt(attachmentIndex);

            for (int i = 0; i < question.Attachments.Count; i++)
            {
                question.Attachments[i].SortOrder = i + 1;
            }

            await UpdateQuestionAsync(userId, questionIndex, question);
        }

        // Вспомогательные методы
        private int GetNextTempId()
        {
            return System.Threading.Interlocked.Increment(ref _nextTempId);
        }

        private QuestionEditorViewModel CloneQuestion(QuestionEditorViewModel original)
        {
            return new QuestionEditorViewModel
            {
                TempId = original.TempId,
                QuestionId = original.QuestionId,
                OrderNumber = original.OrderNumber,
                Description = original.Description,
                Type = original.Type,
                IsActual = original.IsActual,
                IsExpanded = original.IsExpanded,
                Options = original.Options.Select(o => new AutoQuestionOptionEditorViewModel
                {
                    TempId = o.TempId,
                    OptionId = o.OptionId,
                    OptionText = o.OptionText,
                    IsCorrect = o.IsCorrect,
                    SortOrder = o.SortOrder
                }).ToList(),
                MaxScore = original.MaxScore,
                ModelAnswer = original.ModelAnswer,
                Attachments = original.Attachments.Select(a => new QuestionAttachmentEditorViewModel
                {
                    TempId = a.TempId,
                    AttachmentId = a.AttachmentId,
                    ImageUrl = a.ImageUrl,
                    SortOrder = a.SortOrder
                }).ToList()
            };
        }
    }
}
