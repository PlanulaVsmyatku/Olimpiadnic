using Olimpiadnic.Entities;
using Olimpiadnic.Models.MyOlympiads;
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
        Task<IEnumerable<Olympiad>> GetAllOlympiadsAsync();
        /// <summary>
        /// Возвращает конкретную олимпиаду по ID
        /// </summary>
        Task<Olympiad> GetOlympiadByIdAsync(int olympId);
        /// <summary>
        /// Возвращает только олимпиады, которые ещё не начались или в процессе
        /// </summary>
        Task<IEnumerable<Olympiad>> GetActiveOlympiadsAsync();
        /// <summary>
        /// Возвращает лист активных олимпиад (не завершившихся), раздёленных на группы по <= pageSize если их больше указанного pageSize, учитывая поисковые фильтры
        /// </summary>
        Task<OlympiadPagedResult> GetActiveOlympiadsPagedFilteredAsync(
            OlympiadSearchViewModel? searchModel,
            int pageNumber,
            int pageSize = 12);
        /// <summary>
        /// Возвращает расширенную информацию об олимпиаде
        /// </summary>
        Task<Olympiad?> GetOlympiadWithDetailsAsync(int olympId);
        /// <summary>
        /// Возвращает общее число заданий кокнретной олимпиады
        /// </summary>
        Task<int> GetTotalQuestionsCountAsync(int olympId);
        /// <summary>
        /// Закрепление изменений таблицы олимпиад
        /// </summary>
        Task UpdateOlympiadAsync(Olympiad olympiad);

        #region Создание/Редактирование олимпиад
        /// <summary>
        /// Создание новой олимпиады с вопросами
        /// </summary>
        Task<int> CreateOlympiadAsync(CreateOlympiadViewModel model, int creatorUserId);

        /// <summary>
        /// Обновление существующей олимпиады
        /// </summary>
        Task<bool> UpdateOlympiadAsync(CreateOlympiadViewModel model, int editorUserId);

        /// <summary>
        /// Получение олимпиады для редактирования
        /// </summary>
        Task<CreateOlympiadViewModel?> GetOlympiadForEditAsync(int olympiadId);
        #endregion

        #endregion

        #region Участники
        Task<IEnumerable<OlympiadParticipant>> GetParticipantsByOlympiadIdAsync(int olympId);
        Task<IEnumerable<OlympiadParticipant>> GetParticipantsByUserAndOlympiadIdsAsync(string userId, List<int> olympiadIds);
        Task<bool> RegisterParticipantAsync(int olympiadId, int userId);
        Task<bool> IsUserRegisteredAsync(int olympiadId, int userId);
        Task UpdateParticipantAsync(OlympiadParticipant participant);
        Task<OlympiadParticipant> GetOrCreateParticipantAsync(int olympiadId, int userId);
        #endregion

        #region Вопросы (оригиналы)
        Task<List<Question>> GetQuestionsForParticipationAsync(int olympiadId);
        Task<Question?> GetQuestionWithOptionsAsync(int questionId);
        Task<ManualQuestionsConfig?> GetManualQuestionConfigAsync(int questionId);
        Task<List<QuestionAttachment>> GetQuestionAttachmentsAsync(int questionId);
        #endregion

        #region Снапшоты
        Task<QuestionsSnapshot?> GetQuestionSnapshotByOriginalIdAsync(int olympiadId, int originalQuestionId);
        Task<List<QuestionsSnapshot>> GetQuestionSnapshotsByOlympiadIdAsync(int originalOlympiadId);
        Task<List<AutoQuestionsSnapshot>> GetAutoOptionsSnapshotByQuestionSnapshotIdAsync(int questionSnapshotId);
        Task<ManualQuestionsConfigSnapshot?> GetManualConfigSnapshotByQuestionSnapshotIdAsync(int questionSnapshotId);
        Task<QuestionsSnapshot?> GetQuestionSnapshotByIdAsync(int questionSnapshotId);

        /// <summary>
        /// Получение снапшота олимпиады по оригинальному ID
        /// </summary>
        Task<OlympiadSnapshot?> GetOlympiadSnapshotByOriginalIdAsync(int originalOlympiadId);
        #endregion

        #region Сохранение ответов и проверка
        /// <summary>
        /// Сохранение ответа на вопрос (один вопрос) с автоматической проверкой для auto-типов
        /// </summary>
        /// <returns>Результат проверки с баллами</returns>
        Task<AnswerSaveResult> SaveAnswerAndCheckAsync(int participantId, int questionSnapshotId, object answerData);

        /// <summary>
        /// Получение всех сохранённых ответов участника для олимпиады
        /// </summary>
        Task<List<SubmittedAnswerDto>> GetParticipantAnswersAsync(int participantId);

        /// <summary>
        /// Финальное завершение олимпиады с пересчётом всех баллов
        /// </summary>
        Task<FinalizeResult> FinalizeOlympiadAsync(int participantId);
        #endregion

        #region Результаты участников
        Task<OlympiadResult?> GetParticipantResultAsync(int participantId);
        Task<ParticipantResultsViewModel?> GetParticipantResultsForDisplayAsync(int participantId);
        #endregion

        #region Мои олимпиады
        /// <summary>
        /// Получение олимпиад участника с результатами
        /// </summary>
        Task<MyOlympiadsPagedResult<ParticipantOlympiadViewModel>> GetParticipantOlympiadsAsync(
            int userId,
            MyOlympiadsFilterViewModel? filter,
            int pageNumber,
            int pageSize = 6);

        /// <summary>
        /// Получение олимпиад сотрудника (автор или проверяющий)
        /// </summary>
        Task<MyOlympiadsPagedResult<StaffOlympiadViewModel>> GetStaffOlympiadsAsync(
            int userId,
            MyOlympiadsFilterViewModel? filter,
            int pageNumber,
            int pageSize = 6);

        /// <summary>
        /// Получение всех олимпиад для администратора
        /// </summary>
        Task<MyOlympiadsPagedResult<AdminOlympiadViewModel>> GetAllOlympiadsForAdminAsync(
            MyOlympiadsFilterViewModel? filter,
            int pageNumber,
            int pageSize = 10);
        #endregion

    }

    #region DTOs для ответов
    public class AnswerSaveResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int SubmissionItemId { get; set; }
        public bool IsAuto { get; set; }
        public bool? IsCorrect { get; set; }
        public int? EarnedScore { get; set; }
        public int MaxScore { get; set; }
        public string? CorrectAnswerInfo { get; set; }

        // поля для отображения результатов
        public List<int> CorrectOptionIds { get; set; } = new();
        public List<int> IncorrectOptionIds { get; set; } = new();
        public List<int> SelectedOptionIds { get; set; } = new();
        public bool IsCheckbox { get; set; }
    }

    public class SubmittedAnswerDto
    {
        public int SubmissionItemId { get; set; }
        public int QuestionSnapshotId { get; set; }
        public string QuestionType { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public int? SelectedOptionId { get; set; }
        public string? SelectedOptionText { get; set; }
        public string? ManualAnswer { get; set; }
        public bool? IsCorrect { get; set; }
        public int? ScoreValue { get; set; }
        public int MaxScore { get; set; }
        public string? Commentary { get; set; }
    }

    public class FinalizeResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int TotalScore { get; set; }
        public int AutoScore { get; set; }
        public int ManualPendingCount { get; set; }
    }
    #endregion

    /// <summary>
    /// Результат пагинации для моих олимпиад
    /// </summary>
    public class MyOlympiadsPagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public MyOlympiadsFilterViewModel? Filter { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

}

