using Microsoft.EntityFrameworkCore;
using Olimpiadnic.Data;
using Olimpiadnic.Entities;
using Olimpiadnic.Models.OlympiadModels;
namespace Olimpiadnic.Services.Repos
{
    public class OlympiadRepository : IOlympiadRepository
    {
        private readonly AppDbContext _context;

        public OlympiadRepository(AppDbContext context)
        {
            _context = context;
        }

        #region Олимпиады
        public async Task<IEnumerable<Olympiad>> GetAllOlympiadsAsync()
        {
            var allOlympiads = await _context.Olympiads
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync<Olympiad>();

            return allOlympiads;
        }

        public async Task<Olympiad> GetOlympiadByIdAsync(int olympId)
        {
            var olympiad = await _context.Olympiads
                            .FirstOrDefaultAsync(o => o.OlympId == olympId);
            return olympiad;
        }

        public async Task<IEnumerable<Olympiad>> GetActiveOlympiadsAsync()
        {
            var now = DateTime.UtcNow;

            var activeOlympiads = await _context.Olympiads
                .Where(o => o.EventEnd >= now)
                .OrderBy(o => o.EventStart)
                .ToListAsync();

            return activeOlympiads;
        }

        public async Task<OlympiadPagedResult> GetActiveOlympiadsPagedFilteredAsync(
            OlympiadSearchViewModel? searchModel,
            int pageNumber,
            int pageSize = 12)
        {
            var now = DateTime.UtcNow;

            // Базовый запрос - только активные олимпиады (не завершённые)
            var query = _context.Olympiads
                .Where(o => o.EventEnd >= now);

            // Применяем фильтры из searchModel
            if (searchModel != null)
            {
                // Фильтр по названию
                if (!string.IsNullOrWhiteSpace(searchModel.SearchTitle))
                {
                    query = query.Where(o => o.Title.Contains(searchModel.SearchTitle));
                }

                // Фильтр по дате начала (от)
                if (searchModel.StartDateFrom.HasValue)
                {
                    query = query.Where(o => o.EventStart >= searchModel.StartDateFrom.Value);
                }

                // Фильтр по дате начала (до)
                if (searchModel.StartDateTo.HasValue)
                {
                    query = query.Where(o => o.EventStart <= searchModel.StartDateTo.Value);
                }

                // Фильтр по дате окончания (от)
                if (searchModel.EndDateFrom.HasValue)
                {
                    query = query.Where(o => o.EventEnd >= searchModel.EndDateFrom.Value);
                }

                // Фильтр по дате окончания (до)
                if (searchModel.EndDateTo.HasValue)
                {
                    query = query.Where(o => o.EventEnd <= searchModel.EndDateTo.Value);
                }
            }

            // Сортировка: сначала те, что скоро начнутся
            query = query.OrderBy(o => o.EventStart);

            // Получаем общее количество после фильтрации
            var totalCount = await query.CountAsync();

            // Рассчитываем количество страниц
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Корректируем номер страницы
            if (pageNumber < 1) pageNumber = 1;
            if (pageNumber > totalPages && totalPages > 0) pageNumber = totalPages;

            // Получаем элементы для текущей страницы
            // Получаем данные из БД
            var itemsData = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    o.OlympId,
                    o.Title,
                    o.Description,
                    o.ImageUrl,
                    o.Credentials,
                    o.EventStart,
                    o.EventEnd,
                    o.RegistOpen,
                    o.RegistClosed
                })
                .ToListAsync();

            // Вычисляем статус на клиенте (в памяти)
            var items = itemsData.Select(o => new OlympiadCardViewModel
            {
                OlympiadId = o.OlympId,
                Title = o.Title,
                Description = o.Description ?? string.Empty,
                ImageUrl = o.ImageUrl,
                Credentials = o.Credentials,
                EventStart = o.EventStart,
                EventEnd = o.EventEnd,
                RegistOpen = o.RegistOpen,
                RegistClosed = o.RegistClosed,
                Status = GetStatusForOlympiadStatic(o.EventStart, o.EventEnd, o.RegistOpen, o.RegistClosed),
                IsUserRegistered = false
            }).ToList();

            return new OlympiadPagedResult
            {
                Items = items,
                CurrentPage = pageNumber,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize,
                SearchModel = searchModel //сохраняем параметры поисковой модели
            };
        }

        // Вспомогательный метод для определения статуса олимпиады
        private static string GetStatusForOlympiadStatic(DateTime eventStart, DateTime eventEnd, DateTime registOpen, DateTime registClosed)
        {
            var now = DateTime.UtcNow;

            if (now >= registOpen && now <= registClosed && eventEnd >= now)
                return "Регистрация открыта";
            if (now >= eventStart && now <= eventEnd)
                return "Идёт";
            if (now > eventEnd)
                return "Завершена";
            if (now < registOpen)
                return "Регистрация скоро";

            return "Скоро";
        }

        public async Task<Olympiad?> GetOlympiadWithDetailsAsync(int olympId)
        {
            return await _context.Olympiads
                .Include(o => o.Questions.Where(q => q.IsActual))
                .FirstOrDefaultAsync(o => o.OlympId == olympId);
        }

        public async Task<int> GetTotalQuestionsCountAsync(int olympId)
        {
            return await _context.Questions
                .Where(q => q.OlympId == olympId && q.IsActual)
                .CountAsync();
        }

        #endregion

        #region Участники
        public async Task<IEnumerable<OlympiadParticipant>> GetParticipantsByOlympiadIdAsync(int olympId)
        {
            var participants = await _context.OlympiadParticipants
                .Where(p => p.OlympId == olympId)
                .ToListAsync();

            return participants;
        }

        public async Task<IEnumerable<OlympiadParticipant>> GetParticipantsByUserAndOlympiadIdsAsync(string userId, List<int> olympiadIds)
        {
            return await _context.OlympiadParticipants
                .Where(p => p.UserId == int.Parse(userId) && olympiadIds.Contains(p.OlympId))
                .ToListAsync();
        }

        public async Task<bool> RegisterParticipantAsync(int olympiadId, int userId)
        {
            try
            {
                // Проверяем, не зарегистрирован ли уже
                var existing = await _context.OlympiadParticipants
                    .FirstOrDefaultAsync(p => p.OlympId == olympiadId && p.UserId == userId);

                if (existing != null)
                    return false;

                var participant = new OlympiadParticipant
                {
                    OlympId = olympiadId,
                    UserId = userId,
                    RegDate = DateTime.Now,
                    Status = "registered"
                };

                _context.OlympiadParticipants.Add(participant);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                return false;
            }
        }

        public async Task<bool> IsUserRegisteredAsync(int olympiadId, int userId)
        {
            return await _context.OlympiadParticipants
                .AnyAsync(p => p.OlympId == olympiadId && p.UserId == userId);
        }

        public async Task UpdateParticipantAsync(OlympiadParticipant participant)
        {
            _context.OlympiadParticipants.Update(participant);
            await _context.SaveChangesAsync();
        }
        #endregion

        #region Вопросы олимпиады
        public async Task<List<Question>> GetQuestionsForParticipationAsync(int olympiadId)
        {
            return await _context.Questions
                .Where(q => q.OlympId == olympiadId && q.IsActual)
                .OrderBy(q => q.QuestionOrder)
                .ToListAsync();
        }

        public async Task<Question?> GetQuestionWithOptionsAsync(int questionId)
        {
            return await _context.Questions
                .Include(q => q.AutoQuestions.OrderBy(a => a.SortOrder))
                .FirstOrDefaultAsync(q => q.QuestId == questionId);
        }

        public async Task<ManualQuestionsConfig?> GetManualQuestionConfigAsync(int questionId)
        {
            return await _context.ManualQuestionsConfigs
                .FirstOrDefaultAsync(m => m.QuestId == questionId);
        }

        public async Task<OlympiadParticipant> GetOrCreateParticipantAsync(int olympiadId, int userId)
        {
            var participant = await _context.OlympiadParticipants
                .FirstOrDefaultAsync(p => p.OlympId == olympiadId && p.UserId == userId);

            if (participant == null)
            {
                participant = new OlympiadParticipant
                {
                    OlympId = olympiadId,
                    UserId = userId,
                    RegDate = DateTime.Now,
                    Status = "registered"
                };
                _context.OlympiadParticipants.Add(participant);
                await _context.SaveChangesAsync();
            }

            return participant;
        }

        #endregion
        /*
        #region Снапшоты

        public async Task<QuestionsSnapshot?> GetQuestionSnapshotByOriginalIdAsync(int olympiadId, int originalQuestionId)
        {
            // Сначала получаем снимок олимпиады
            var olympiadSnapshot = await _context.OlympiadSnapshots
                .FirstOrDefaultAsync(os => os.OlympId == olympiadId);

            if (olympiadSnapshot == null) return null;

            // Получаем снимок вопроса по оригинальному ID
            return await _context.QuestionsSnapshots
                .FirstOrDefaultAsync(qs => qs.OlympSnapId == olympiadSnapshot.OlympSnapId
                                           && qs.QuestIdOriginal == originalQuestionId);
        }

        public async Task<List<QuestionsSnapshot>> GetQuestionSnapshotsByOlympiadIdAsync(int olympiadId)
        {
            var olympiadSnapshot = await _context.OlympiadSnapshots
                .FirstOrDefaultAsync(os => os.OlympId == olympiadId);

            if (olympiadSnapshot == null) return new List<QuestionsSnapshot>();

            return await _context.QuestionsSnapshots
                .Where(qs => qs.OlympSnapId == olympiadSnapshot.OlympSnapId)
                .OrderBy(qs => qs.QuestOrderSnapshot)
                .ToListAsync();
        }

        public async Task<List<AutoQuestionsSnapshot>> GetAutoOptionsSnapshotByQuestionSnapshotIdAsync(int questionSnapshotId)
        {
            return await _context.AutoQuestionsSnapshots
                .Where(a => a.QuestSnapshotId == questionSnapshotId)
                .OrderBy(a => a.SortOrder)
                .ToListAsync();
        }

        public async Task<ManualQuestionsConfigSnapshot?> GetManualConfigSnapshotByQuestionSnapshotIdAsync(int questionSnapshotId)
        {
            return await _context.ManualQuestionsConfigSnapshots
                .FirstOrDefaultAsync(m => m.QuestSnapshotId == questionSnapshotId);
        }

        #endregion
        /*
        #region Сохранение ответов
        
        public async Task SaveAnswerSubmissionAsync(int participantId, int questionSnapshotId, object answerData)
        {
            // Проверяем, есть ли уже запись для этого вопроса
            var existingResult = await _context.OlympiadResults
                .Include(r => r.SubmissionItems)
                .FirstOrDefaultAsync(r => r.ParticipantId == participantId);

            if (existingResult == null)
            {
                existingResult = new OlympiadResult
                {
                    ParticipantId = participantId,
                    SubmissionItems = new List<SubmissionItem>()
                };
                _context.OlympiadResults.Add(existingResult);
                await _context.SaveChangesAsync();
            }

            // Проверяем, есть ли уже ответ на этот вопрос
            var existingItem = existingResult.SubmissionItems
                .FirstOrDefault(si => si.QuestSnapshotId == questionSnapshotId);

            if (existingItem != null)
            {
                // Обновляем существующий ответ
                if (answerData is List<int> selectedOptionIds)
                {
                    // Auto-вопрос - обновляем AutoSubmissionResult
                    var autoResult = await _context.AutoSubmissionResults
                        .FirstOrDefaultAsync(a => a.SubmissionItemId == existingItem.SubmissionItemsId);

                    if (autoResult != null)
                    {
                        autoResult.SelectedOptionId = selectedOptionIds.FirstOrDefault(); // Для radio
                        autoResult.IsCorrect = false; // Пока не проверяем
                        _context.AutoSubmissionResults.Update(autoResult);
                    }
                }
                else if (answerData is string manualAnswer)
                {
                    // Manual-вопрос - обновляем ManualSubmissionResult
                    var manualResult = await _context.ManualSubmissionResults
                        .FirstOrDefaultAsync(m => m.SubmissionItemId == existingItem.SubmissionItemsId);

                    if (manualResult != null)
                    {
                        manualResult.AnswerText = manualAnswer;
                        manualResult.ScoreValue = null; // Пока не проверено
                        manualResult.Commentary = null;
                        _context.ManualSubmissionResults.Update(manualResult);
                    }
                }
            }
            else
            {
                // Создаём новый ответ
                var submissionItem = new SubmissionItem
                {
                    ParticipantId = participantId,
                    QuestSnapshotId = questionSnapshotId,
                    Type = answerData is List<int> ? "auto" : "manual"
                };

                _context.SubmissionItems.Add(submissionItem);
                await _context.SaveChangesAsync(); // Чтобы получить SubmissionItemsId

                if (answerData is List<int> selectedOptionIds)
                {
                    // Для radio берём первый, для checkbox нужно несколько записей
                    var autoResult = new AutoSubmissionResult
                    {
                        SubmissionItemId = submissionItem.SubmissionItemsId,
                        SelectedOptionId = selectedOptionIds.FirstOrDefault(),
                        IsCorrect = false //пока не проверяем - а почему бы сразу не авто-првоерить??
                    };
                    _context.AutoSubmissionResults.Add(autoResult);
                }
                else if (answerData is string manualAnswer)
                {
                    var manualResult = new ManualSubmissionResult
                    {
                        SubmissionItemId = submissionItem.SubmissionItemsId,
                        AnswerText = manualAnswer,
                        ScoreValue = null,
                        Commentary = null
                    };
                    _context.ManualSubmissionResults.Add(manualResult);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task CompleteOlympiadAsync(int participantId, int totalScore)
        {
            var participant = await _context.OlympiadParticipants
                .FirstOrDefaultAsync(p => p.ParticipantId == participantId);

            if (participant != null)
            {
                participant.Status = "completed";
                participant.CompletedAt = DateTime.Now;
                _context.OlympiadParticipants.Update(participant);
            }

            var result = await _context.OlympiadResults
                .FirstOrDefaultAsync(r => r.ParticipantId == participantId);

            if (result != null)
            {
                result.TotalScore = totalScore;
                _context.OlympiadResults.Update(result);
            }

            await _context.SaveChangesAsync();
        }

        #endregion
        */
    }
}
