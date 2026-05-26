using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Olimpiadnic.Extensions;
using Olimpiadnic.Models.RoleModels;

namespace Olimpiadnic.Services.Session
{
    public interface IOlympiadEditorSessionService
    {
        Task<CreateOlympiadViewModel?> GetSessionAsync(int userId); //создать сессию с ключём от ID пользователя сотрудника
        Task SaveSessionAsync(int userId, CreateOlympiadViewModel model); // сохранить состояние сессии. класс расширение работает?
        Task DeleteSessionAsync(int userId);
        bool SessionExists(int userId);

        //Для работы с информацией об олимпиаде
        Task UpdateOlympiadInfoAsync(int userId, CreateOlympiadViewModel updatedInfo);

        // Методы для работы с вопросами
        Task<QuestionEditorViewModel?> GetQuestionAsync(int userId, int questionIndex);
        Task UpdateQuestionAsync(int userId, int questionIndex, QuestionEditorViewModel question);
        Task<int> AddQuestionAsync(int userId);
        Task RemoveQuestionAsync(int userId, int questionIndex);
        Task ReorderQuestionsAsync(int userId, List<int> newOrder);


        //Методы для работы с вариантами ответов для авто-вопросов
        Task<List<AutoQuestionOptionEditorViewModel>> GetQuestionOptionsAsync(int userId, int questionIndex);
        Task AddQuestionOptionAsync(int userId, int questionIndex, QuestionEditorViewModel question);
        Task RemoveQuestionOptionAsync(int userId, int questionIndex, int optionindex);
        Task ReorderQuestionOptionsAsync(int userId, int questionIndex, List<AutoQuestionOptionEditorViewModel> optionsList);
        //Для работы с вложениями вопросов
        Task AddAttachmentAsync(int userId, int questionIndex, string imageUrl);
        Task RemoveAttachmentAsync(int userId, int questionIndex, int attachmentIndex);

    }

}