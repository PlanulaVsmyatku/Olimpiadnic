using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Olimpiadnic.Extensions;
using Olimpiadnic.Models.RoleModels;

namespace Olimpiadnic.Services.Repos
{
    public interface IOlympiadEditorSessionService
    {
        Task<CreateOlympiadViewModel?> GetSessionAsync(int userId);
        Task SaveSessionAsync(int userId, CreateOlympiadViewModel model);
        Task DeleteSessionAsync(int userId);
        bool SessionExists(int userId);

        // Методы для работы с вопросами
        Task<QuestionEditorViewModel?> GetQuestionAsync(int userId, int questionIndex);
        Task UpdateQuestionAsync(int userId, int questionIndex, QuestionEditorViewModel question);
        Task<int> AddQuestionAsync(int userId);
        Task RemoveQuestionAsync(int userId, int questionIndex);
        Task ReorderQuestionsAsync(int userId, List<int> newOrder);
    }

}