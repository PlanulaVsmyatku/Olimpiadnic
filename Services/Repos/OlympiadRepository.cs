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

        public async Task<List<QuestionAttachment>> GetQuestionAttachmentsAsync(int questionId)
        {
            return await _context.QuestionAttachments
                .Where(a => a.QuestId == questionId)
                .OrderBy(a => a.SortOrder)
                .ToListAsync();
        }

        #endregion

        #region Снапшоты

        public async Task<QuestionsSnapshot?> GetQuestionSnapshotByOriginalIdAsync(int olympiadId, int originalQuestionId)
        {
            // Сначала получаем снимок олимпиады
            var olympiadSnapshot = await _context.OlympiadSnapshots
                .FirstOrDefaultAsync(os => os.OriginalOlympId == olympiadId);

            if (olympiadSnapshot == null) return null;

            // Получаем снимок вопроса по оригинальному ID
            return await _context.QuestionsSnapshots
                .FirstOrDefaultAsync(qs => qs.OlympSnapId == olympiadSnapshot.OlympSnapId
                                           && qs.QuestIdOriginal == originalQuestionId);
        }

        public async Task<List<QuestionsSnapshot>> GetQuestionSnapshotsByOlympiadIdAsync(int olympiadId)
        {
            var olympiadSnapshot = await _context.OlympiadSnapshots
                .FirstOrDefaultAsync(os => os.OriginalOlympId == olympiadId);

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

        public async Task<QuestionsSnapshot?> GetQuestionSnapshotByIdAsync(int questionSnapshotId)
        {
            return await _context.QuestionsSnapshots
                .FirstOrDefaultAsync(qs => qs.QuestSnapshotId == questionSnapshotId);
        }

        #endregion

        #region Сохранение ответов

        public async Task SaveAnswerSubmissionAsync(int participantId, int questionSnapshotId, object answerData)
        {
            // Находим или создаём OlympiadResult
            var result = await _context.OlympiadResults
                .Include(r => r.SubmissionItems)
                .FirstOrDefaultAsync(r => r.ParticipantId == participantId);

            if (result == null)
            {
                result = new OlympiadResult
                {
                    ParticipantId = participantId,
                    TotalScore = null
                };
                _context.OlympiadResults.Add(result);
                await _context.SaveChangesAsync();
            }

            // Ищем существующий SubmissionItem
            var existingItem = result.SubmissionItems
                .FirstOrDefault(si => si.QuestSnapshotId == questionSnapshotId);

            if (existingItem != null)
            {
                // Обновляем существующий
                if (answerData is List<int> selectedIds && selectedIds.Any())
                {
                    // Удаляем старые результаты
                    if (existingItem.AutoSubmissionResults.Any())
                    {
                        _context.AutoSubmissionResults.RemoveRange(existingItem.AutoSubmissionResults);
                    }

                    // Добавляем новый
                    _context.AutoSubmissionResults.Add(new AutoSubmissionResult
                    {
                        SubmissionItemId = existingItem.SubmissionItemsId,
                        SelectedOptionId = selectedIds.First(),
                        IsCorrect = null
                    });
                }
                else if (answerData is string manualAnswer)
                {
                    if (existingItem.ManualSubmissionResult != null)
                    {
                        existingItem.ManualSubmissionResult.AnswerText = manualAnswer;
                        existingItem.ManualSubmissionResult.ScoreValue = null;
                        existingItem.ManualSubmissionResult.Commentary = null;
                    }
                }
            }
            else
            {
                // Создаём новый
                var item = new SubmissionItem
                {
                    ResultsId = result.ResultsId,  
                    QuestSnapshotId = questionSnapshotId,
                    Type = answerData is List<int> ? "auto" : "manual"
                };
                _context.SubmissionItems.Add(item);
                await _context.SaveChangesAsync();

                if (answerData is List<int> selectedIds && selectedIds.Any())
                {
                    _context.AutoSubmissionResults.Add(new AutoSubmissionResult
                    {
                        SubmissionItemId = item.SubmissionItemsId,
                        SelectedOptionId = selectedIds.First(),
                        IsCorrect = null
                    });
                }
                else if (answerData is string manualAnswer)
                {
                    _context.ManualSubmissionResults.Add(new ManualSubmissionResult
                    {
                        SubmissionItemId = item.SubmissionItemsId,
                        AnswerText = manualAnswer,
                        ScoreValue = null,
                        Commentary = null
                    });
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

        #region Результаты участников
        public async Task<OlympiadResult?> GetParticipantResultAsync(int participantId)
        {
            return await _context.OlympiadResults
                .Include(r => r.SubmissionItems)
                    .ThenInclude(si => si.AutoSubmissionResults)
                .Include(r => r.SubmissionItems)
                    .ThenInclude(si => si.ManualSubmissionResult)
                .FirstOrDefaultAsync(r => r.ParticipantId == participantId);
        }

        public async Task<ParticipantResultsViewModel?> GetParticipantResultsForDisplayAsync(int participantId)
        {
            var result = await _context.OlympiadResults
                .Include(r => r.Participant)
                    .ThenInclude(p => p.User)
                        .ThenInclude(u => u.UserProfile)
                .Include(r => r.SubmissionItems)
                    .ThenInclude(si => si.AutoSubmissionResults)
                .Include(r => r.SubmissionItems)
                    .ThenInclude(si => si.ManualSubmissionResult)
                .Include(r => r.SubmissionItems)
                    .ThenInclude(si => si.QuestSnapshot)
                        .ThenInclude(qs => qs.AutoQuestionsSnapshots)
                .Include(r => r.SubmissionItems)
                    .ThenInclude(si => si.QuestSnapshot)
                        .ThenInclude(qs => qs.OlympSnap)
                .FirstOrDefaultAsync(r => r.ParticipantId == participantId);

            if (result == null) return null;

            var viewModel = new ParticipantResultsViewModel
            {
                OlympiadId = result.Participant.OlympId,
                OlympiadTitle = result.Participant.Olymp?.Title ?? "Олимпиада",
                ParticipantName = result.Participant.User?.UserProfile?.FullName ?? result.Participant.User?.Login ?? "Участник",
                TotalScore = result.TotalScore ?? 0,
                MaxPossibleScore = await CalculateTotalMaxScoreForParticipantAsync(result.ParticipantId),
                CompletedAt = result.Participant.CompletedAt,
                StartedAt = result.Participant.StartedAt,
                Status = result.Participant.Status,
                QuestionResults = new List<QuestionResultViewModel>()
            };

            // Получаем все вопросы (оригиналы) для порядка
            var questions = await GetQuestionsForParticipationAsync(viewModel.OlympiadId);
            var questionsDict = questions.ToDictionary(q => q.QuestId);

            foreach (var submissionItem in result.SubmissionItems.OrderBy(s => s.QuestSnapshot.QuestOrderSnapshot))
            {
                var snapshot = submissionItem.QuestSnapshot;
                var originalQuestion = snapshot.QuestIdOriginal.HasValue && questionsDict.ContainsKey(snapshot.QuestIdOriginal.Value)
                    ? questionsDict[snapshot.QuestIdOriginal.Value]
                    : null;

                // Получаем все варианты ответов для auto-вопросов
                List<OptionDisplayViewModel> allOptions = new();
                if (snapshot.QuestionTypeSnapshot.StartsWith("auto"))
                {
                    var options = await _context.AutoQuestionsSnapshots
                        .Where(a => a.QuestSnapshotId == snapshot.QuestSnapshotId)
                        .OrderBy(a => a.SortOrder)
                        .ToListAsync();

                    allOptions = options.Select(o => new OptionDisplayViewModel
                    {
                        OptionId = o.QuestOptionId,
                        OptionText = o.OptionText,
                        IsCorrect = o.IsCorrect
                    }).ToList();
                }

                var questionResult = new QuestionResultViewModel
                {
                    QuestionId = snapshot.QuestIdOriginal ?? 0,
                    OrderNumber = snapshot.QuestOrderSnapshot,
                    QuestionText = snapshot.QuestionDescSnapshot,
                    QuestionType = snapshot.QuestionTypeSnapshot,
                    MaxScore = await GetMaxScoreForQuestionSnapshotAsync(snapshot.QuestSnapshotId),
                    AllOptions = allOptions
                };

                if (submissionItem.Type == "auto" && submissionItem.AutoSubmissionResults.Any())
                {
                    // Получаем выбранные ID из БД (они уже сохранены)
                    var selectedIds = submissionItem.AutoSubmissionResults.Select(r => r.SelectedOptionId).ToList();

                    // Определяем правильные и неправильные ID
                    var correctIds = allOptions.Where(o => o.IsCorrect).Select(o => o.OptionId).ToList();
                    var incorrectIds = allOptions.Where(o => !o.IsCorrect).Select(o => o.OptionId).ToList();

                    // Строгая проверка
                    bool isFullyCorrect = correctIds.All(id => selectedIds.Contains(id))
                        && !selectedIds.Any(id => incorrectIds.Contains(id));

                    questionResult.SelectedOptionIds = selectedIds;
                    questionResult.IsCorrect = isFullyCorrect;
                    questionResult.Score = isFullyCorrect ? 1 : 0; // Для auto всегда максимум 1 балл
                    questionResult.Status = isFullyCorrect ? "correct" : "incorrect";

                    // Формируем отображаемый ответ
                    var selectedOptionsText = allOptions
                        .Where(o => selectedIds.Contains(o.OptionId))
                        .Select(o => o.OptionText);
                    questionResult.UserAnswer = string.Join(", ", selectedOptionsText);

                    // Формируем информацию о правильных ответах
                    var correctOptionsText = allOptions
                        .Where(o => o.IsCorrect)
                        .Select(o => o.OptionText);
                    questionResult.CorrectAnswerInfo = string.Join(", ", correctOptionsText);
                }
                else if (submissionItem.Type == "manual" && submissionItem.ManualSubmissionResult != null)
                {
                    var manualResult = submissionItem.ManualSubmissionResult;
                    questionResult.UserAnswer = manualResult.AnswerText;
                    questionResult.Score = manualResult.ScoreValue;
                    questionResult.Commentary = manualResult.Commentary;
                    questionResult.Status = manualResult.ScoreValue.HasValue ? "reviewed" : "pending";
                    // Для manual MaxScore уже установлен из конфига
                }

                viewModel.QuestionResults.Add(questionResult);
            }

            viewModel.AutoScore = viewModel.QuestionResults
                .Where(q => q.QuestionType.StartsWith("auto") && q.Score.HasValue)
                .Sum(q => q.Score.Value);

            viewModel.ManualScore = viewModel.QuestionResults
                .Where(q => q.QuestionType == "manual" && q.Score.HasValue)
                .Sum(q => q.Score.Value);

            return viewModel;
        }

        // Вспомогательный метод для подсчёта максимального балла
        private async Task<int> CalculateTotalMaxScoreForParticipantAsync(int participantId)
        {
            var result = await _context.OlympiadResults
                .Include(r => r.SubmissionItems)
                .ThenInclude(si => si.QuestSnapshot)
                .FirstOrDefaultAsync(r => r.ParticipantId == participantId);

            if (result == null) return 0;

            int total = 0;
            foreach (var item in result.SubmissionItems)
            {
                total += await GetMaxScoreForQuestionSnapshotAsync(item.QuestSnapshotId);
            }
            return total;
        }
        #endregion

        #region Сохранение ответов и проверка

        /// <summary>
        /// Получение снапшота олимпиады по оригинальному ID
        /// </summary>
        public async Task<OlympiadSnapshot?> GetOlympiadSnapshotByOriginalIdAsync(int originalOlympiadId)
        {
            return await _context.OlympiadSnapshots
                .FirstOrDefaultAsync(os => os.OriginalOlympId == originalOlympiadId);
        }

        /// <summary>
        /// Сохранение ответа на вопрос с автоматической проверкой для auto-типов
        /// </summary>
        public async Task<AnswerSaveResult> SaveAnswerAndCheckAsync(int participantId, int questionSnapshotId, object answerData)
        {
            try
            {
                // 1. Получаем снапшот вопроса и его тип
                var questionSnapshot = await _context.QuestionsSnapshots
                    .FirstOrDefaultAsync(qs => qs.QuestSnapshotId == questionSnapshotId);

                if (questionSnapshot == null)
                    return new AnswerSaveResult { Success = false, ErrorMessage = "Вопрос не найден" };

                // 2. Получаем или создаём результат участника
                var result = await _context.OlympiadResults
                    .FirstOrDefaultAsync(r => r.ParticipantId == participantId);

                if (result == null)
                {
                    result = new OlympiadResult
                    {
                        ParticipantId = participantId,
                        TotalScore = null
                    };
                    _context.OlympiadResults.Add(result);
                    await _context.SaveChangesAsync();
                }

                // 3. Ищем существующий SubmissionItem или создаём новый
                var submissionItem = await _context.SubmissionItems
                    .Include(si => si.AutoSubmissionResults)
                    .Include(si => si.ManualSubmissionResult)
                    .FirstOrDefaultAsync(si => si.ResultsId == result.ResultsId && si.QuestSnapshotId == questionSnapshotId);

                bool isNewItem = false;
                if (submissionItem == null)
                {
                    submissionItem = new SubmissionItem
                    {
                        ResultsId = result.ResultsId,
                        QuestSnapshotId = questionSnapshotId,
                        Type = questionSnapshot.QuestionTypeSnapshot.StartsWith("auto") ? "auto" : "manual"
                    };
                    _context.SubmissionItems.Add(submissionItem);
                    isNewItem = true;
                    await _context.SaveChangesAsync(); // чтобы получить SubmissionItemsId
                }

                // 4. Обрабатываем в зависимости от типа вопроса
                bool isAuto = questionSnapshot.QuestionTypeSnapshot.StartsWith("auto");
                int maxScore = await GetMaxScoreForQuestionSnapshotAsync(questionSnapshotId);

                var saveResult = new AnswerSaveResult
                {
                    Success = true,
                    SubmissionItemId = submissionItem.SubmissionItemsId,
                    IsAuto = isAuto,
                    MaxScore = maxScore
                };

                if (isAuto)
                {
                    // Получаем выбранные ID (поддерживаем список для checkbox)
                    List<int> selectedOptionIds = new List<int>();

                    if (answerData is List<int> ids && ids.Any())
                        selectedOptionIds = ids;
                    else if (answerData is int singleId && singleId > 0)
                        selectedOptionIds = new List<int> { singleId };
                    else if (answerData is string strId && int.TryParse(strId, out int parsedId))
                        selectedOptionIds = new List<int> { parsedId };

                    // Удаляем старые результаты
                    if (submissionItem.AutoSubmissionResults.Any())
                        _context.AutoSubmissionResults.RemoveRange(submissionItem.AutoSubmissionResults);

                    // Проверяем правильность (поддерживает множественный выбор)
                    var checkResult = await CheckAutoAnswerAsync(questionSnapshotId, selectedOptionIds);

                    // Сохраняем каждый выбранный вариант
                    foreach (var selectedId in selectedOptionIds)
                    {
                        _context.AutoSubmissionResults.Add(new AutoSubmissionResult
                        {
                            SubmissionItemId = submissionItem.SubmissionItemsId,
                            SelectedOptionId = selectedId,
                            IsCorrect = checkResult.IsCorrect // Все выбранные варианты помечаются одинаково
                        });
                    }

                    saveResult.IsCorrect = checkResult.IsCorrect;
                    saveResult.EarnedScore = checkResult.EarnedScore;
                    saveResult.CorrectOptionIds = checkResult.CorrectOptionIds;
                    saveResult.IncorrectOptionIds = checkResult.IncorrectOptionIds;
                    saveResult.IsCheckbox = checkResult.IsCheckbox;
                    saveResult.SelectedOptionIds = selectedOptionIds;
                }
                else // manual
                {
                    var manualAnswer = answerData as string ?? answerData?.ToString() ?? string.Empty;

                    if (submissionItem.ManualSubmissionResult != null)
                    {
                        submissionItem.ManualSubmissionResult.AnswerText = manualAnswer;
                        // При изменении ответа сбрасываем проверку
                        submissionItem.ManualSubmissionResult.ScoreValue = null;
                        submissionItem.ManualSubmissionResult.Commentary = null;
                    }
                    else
                    {
                        _context.ManualSubmissionResults.Add(new ManualSubmissionResult
                        {
                            SubmissionItemId = submissionItem.SubmissionItemsId,
                            AnswerText = manualAnswer,
                            ScoreValue = null,
                            Commentary = null
                        });
                    }

                    saveResult.EarnedScore = null; // manual ждёт проверки от преподавателя
                    saveResult.IsCorrect = null;
                }

                await _context.SaveChangesAsync();

                // 5. Обновляем общий балл в OlympiadResult (только auto-баллы)
                if (isAuto)
                {
                    await RecalculateTotalScoreAsync(result.ResultsId);
                }

                return saveResult;
            }
            catch (Exception ex)
            {
                return new AnswerSaveResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Проверка auto-ответа (поддерживает и radio, и checkbox)
        /// </summary>
        private async Task<AutoCheckResult> CheckAutoAnswerAsync(int questionSnapshotId, List<int> selectedOptionIds)
        {
            if (selectedOptionIds == null || !selectedOptionIds.Any())
                return new AutoCheckResult { IsCorrect = false, EarnedScore = 0, CorrectOptionIds = new List<int>() };

            // Получаем все варианты ответов для этого вопроса
            var options = await _context.AutoQuestionsSnapshots
                .Where(a => a.QuestSnapshotId == questionSnapshotId)
                .ToListAsync();

            var correctOptionIds = options.Where(o => o.IsCorrect).Select(o => o.QuestOptionId).ToList();
            var incorrectOptionIds = options.Where(o => !o.IsCorrect).Select(o => o.QuestOptionId).ToList();

            // Определяем тип вопроса (по количеству правильных ответов)
            bool isCheckbox = correctOptionIds.Count > 1;

            AutoCheckResult result = new AutoCheckResult
            {
                CorrectOptionIds = correctOptionIds,
                IncorrectOptionIds = incorrectOptionIds,
                IsCheckbox = isCheckbox
            };

            //промашка - а что если 3 варианта в чекбоксе а правильный только один?
            if (!isCheckbox)
            {
                // Radio: простой режим
                result.IsCorrect = selectedOptionIds.Count == 1 && selectedOptionIds.First() == correctOptionIds.First();
                result.EarnedScore = result.IsCorrect ? 1 : 0; // maxScore = 1 для radio
            }
            else
            {
                // Checkbox: строгий режим (все правильные выбраны, ни одного неправильного)
                bool hasAllCorrect = correctOptionIds.All(correctId => selectedOptionIds.Contains(correctId));
                bool hasAnyIncorrect = selectedOptionIds.Any(selectedId => incorrectOptionIds.Contains(selectedId));

                result.IsCorrect = hasAllCorrect && !hasAnyIncorrect;
                result.EarnedScore = result.IsCorrect ? correctOptionIds.Count : 0; // maxScore = количество правильных вариантов
            }

            return result;
        }

        // Вспомогательный класс для результата проверки
        private class AutoCheckResult
        {
            public bool IsCorrect { get; set; }
            public int EarnedScore { get; set; }
            public List<int> CorrectOptionIds { get; set; } = new();
            public List<int> IncorrectOptionIds { get; set; } = new();
            public bool IsCheckbox { get; set; }
        }

        /// <summary>
        /// Получение максимального балла для вопроса по снапшоту
        /// </summary>
        // Вспомогательный метод для получения максимального балла вопроса
        private async Task<int> GetMaxScoreForQuestionSnapshotAsync(int questionSnapshotId)
        {
            // Для auto-вопросов всегда 1
            var questionSnapshot = await _context.QuestionsSnapshots
                .FirstOrDefaultAsync(qs => qs.QuestSnapshotId == questionSnapshotId);

            if (questionSnapshot != null && questionSnapshot.QuestionTypeSnapshot.StartsWith("auto"))
                return 1;

            // Для manual - из конфига
            var manualConfig = await _context.ManualQuestionsConfigSnapshots
                .FirstOrDefaultAsync(m => m.QuestSnapshotId == questionSnapshotId);

            return manualConfig?.MaxScore ?? 0;
        }

        private int GetMaxScoreForQuestion(int questionSnapshotId)
        {
            // Этот метод синхронный, используем асинхронную версию выше
            return 1;
        }


        /// <summary>
        /// Пересчёт общего балла участника (сумма всех auto-баллов + проверенных manual)
        /// </summary>
        private async Task RecalculateTotalScoreAsync(int resultsId)
        {
            var result = await _context.OlympiadResults
                .Include(r => r.SubmissionItems)
                    .ThenInclude(si => si.AutoSubmissionResults)
                .Include(r => r.SubmissionItems)
                    .ThenInclude(si => si.ManualSubmissionResult)
                .FirstOrDefaultAsync(r => r.ResultsId == resultsId);

            if (result == null) return;

            int totalScore = 0;

            foreach (var item in result.SubmissionItems)
            {
                if (item.Type == "auto" && item.AutoSubmissionResults.Any())
                {
                    var autoResult = item.AutoSubmissionResults.First();
                    if (autoResult.IsCorrect == true)
                    {
                        // Получаем maxScore для этого вопроса
                        int maxScore = await GetMaxScoreForQuestionSnapshotAsync(item.QuestSnapshotId);
                        totalScore += maxScore;
                    }
                }
                else if (item.Type == "manual" && item.ManualSubmissionResult != null && item.ManualSubmissionResult.ScoreValue.HasValue)
                {
                    totalScore += item.ManualSubmissionResult.ScoreValue.Value;
                }
            }

            result.TotalScore = totalScore;
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Получение всех сохранённых ответов участника для олимпиады
        /// </summary>
        public async Task<List<SubmittedAnswerDto>> GetParticipantAnswersAsync(int participantId)
        {
            var result = await _context.OlympiadResults
                .Include(r => r.SubmissionItems)
                    .ThenInclude(si => si.AutoSubmissionResults)
                .Include(r => r.SubmissionItems)
                    .ThenInclude(si => si.ManualSubmissionResult)
                .Include(r => r.SubmissionItems)
                    .ThenInclude(si => si.QuestSnapshot)
                .FirstOrDefaultAsync(r => r.ParticipantId == participantId);

            if (result == null) return new List<SubmittedAnswerDto>();

            var answers = new List<SubmittedAnswerDto>();

            foreach (var item in result.SubmissionItems)
            {
                var dto = new SubmittedAnswerDto
                {
                    SubmissionItemId = item.SubmissionItemsId,
                    QuestionSnapshotId = item.QuestSnapshotId,
                    QuestionType = item.Type,
                    QuestionText = item.QuestSnapshot?.QuestionDescSnapshot ?? string.Empty,
                    MaxScore = await GetMaxScoreForQuestionSnapshotAsync(item.QuestSnapshotId)
                };

                if (item.Type == "auto" && item.AutoSubmissionResults.Any())
                {
                    var autoResult = item.AutoSubmissionResults.First();
                    dto.SelectedOptionId = autoResult.SelectedOptionId;
                    dto.IsCorrect = autoResult.IsCorrect;
                    dto.ScoreValue = autoResult.IsCorrect == true ? dto.MaxScore : 0;

                    // Получаем текст варианта
                    var option = await _context.AutoQuestionsSnapshots
                        .FirstOrDefaultAsync(a => a.QuestOptionId == autoResult.SelectedOptionId);
                    dto.SelectedOptionText = option?.OptionText;
                }
                else if (item.Type == "manual" && item.ManualSubmissionResult != null)
                {
                    dto.ManualAnswer = item.ManualSubmissionResult.AnswerText;
                    dto.ScoreValue = item.ManualSubmissionResult.ScoreValue;
                    dto.Commentary = item.ManualSubmissionResult.Commentary;
                }

                answers.Add(dto);
            }

            return answers;
        }

        /// <summary>
        /// Финальное завершение олимпиады с пересчётом всех баллов
        /// </summary>
        public async Task<FinalizeResult> FinalizeOlympiadAsync(int participantId)
        {
            try
            {
                var participant = await _context.OlympiadParticipants
                    .FirstOrDefaultAsync(p => p.ParticipantId == participantId);

                if (participant == null)
                    return new FinalizeResult { Success = false, ErrorMessage = "Участник не найден" };

                if (participant.Status == "completed")
                    return new FinalizeResult { Success = false, ErrorMessage = "Олимпиада уже завершена" };

                // Пересчитываем итоговый балл
                var result = await _context.OlympiadResults
                    .Include(r => r.SubmissionItems)
                        .ThenInclude(si => si.AutoSubmissionResults)
                    .Include(r => r.SubmissionItems)
                        .ThenInclude(si => si.ManualSubmissionResult)
                    .FirstOrDefaultAsync(r => r.ParticipantId == participantId);

                int autoScore = 0;
                int manualPendingCount = 0;

                if (result != null)
                {
                    foreach (var item in result.SubmissionItems)
                    {
                        if (item.Type == "auto" && item.AutoSubmissionResults.Any())
                        {
                            var autoResult = item.AutoSubmissionResults.First();
                            if (autoResult.IsCorrect == true)
                            {
                                int maxScore = await GetMaxScoreForQuestionSnapshotAsync(item.QuestSnapshotId);
                                autoScore += maxScore;
                            }
                        }
                        else if (item.Type == "manual")
                        {
                            if (item.ManualSubmissionResult?.ScoreValue == null)
                                manualPendingCount++;
                        }
                    }

                    result.TotalScore = autoScore; // manual баллы добавятся позже при проверке
                    _context.OlympiadResults.Update(result);
                }

                // Обновляем статус участника
                participant.Status = "completed";
                participant.CompletedAt = DateTime.Now;
                _context.OlympiadParticipants.Update(participant);

                await _context.SaveChangesAsync();

                return new FinalizeResult
                {
                    Success = true,
                    TotalScore = autoScore,
                    AutoScore = autoScore,
                    ManualPendingCount = manualPendingCount
                };
            }
            catch (Exception ex)
            {
                return new FinalizeResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        #endregion


    }
}
