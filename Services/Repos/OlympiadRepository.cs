using Microsoft.EntityFrameworkCore;
using Olimpiadnic.Data;
using Olimpiadnic.Entities;
using Olimpiadnic.Models.MyOlympiads;
using Olimpiadnic.Models.OlympiadModels;
using Olimpiadnic.Models.RoleModels;
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
                IsUserRegistered = false,
                IsCompleted = false,
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

        public async Task UpdateOlympiadAsync(Olympiad olympiad)
        {
            _context.Olympiads.Update(olympiad);
            await _context.SaveChangesAsync();
        }


        #region Создание/Редактирование олимпиад

        public async Task<int> CreateOlympiadAsync(CreateOlympiadViewModel model, int creatorUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Создаём олимпиаду
                var olympiad = new Olympiad
                {
                    Title = model.Title,
                    ImageUrl = model.ImageUrl,
                    Description = model.Description,
                    Credentials = model.Credentials,
                    Status = "available",
                    CreatedAt = DateTime.Now,
                    EventStart = model.EventStart,
                    EventEnd = model.EventEnd,
                    RegistOpen = model.RegistOpen,
                    RegistClosed = model.RegistClosed
                };

                _context.Olympiads.Add(olympiad);
                await _context.SaveChangesAsync();

                // 2. Добавляем связь с сотрудником (как автор)
                var olympStaff = new OlympStaff
                {
                    OlympId = olympiad.OlympId,
                    UserId = creatorUserId,
                    OlimpRole = "author",
                    AssignedAt = DateTime.Now
                };
                _context.OlympStaffs.Add(olympStaff);

                // 3. Создаём снапшот олимпиады
                var olympiadSnapshot = new OlympiadSnapshot
                {
                    OriginalOlympId = olympiad.OlympId,
                    Title = olympiad.Title,
                    ImageUrl = olympiad.ImageUrl,
                    Description = olympiad.Description,
                    Credentials = olympiad.Credentials,
                    Status = olympiad.Status,
                    EventStart = olympiad.EventStart,
                    EventEnd = olympiad.EventEnd,
                    RegistOpen = olympiad.RegistOpen,
                    RegistClosed = olympiad.RegistClosed,
                    CreatedAt = olympiad.CreatedAt,
                    CreatedAtSnap = DateTime.Now
                };
                _context.OlympiadSnapshots.Add(olympiadSnapshot);
                await _context.SaveChangesAsync();

                // 4. Создаём вопросы
                foreach (var questionVM in model.Questions.OrderBy(q => q.OrderNumber))
                {
                    var question = new Question
                    {
                        OlympId = olympiad.OlympId,
                        QuestionOrder = questionVM.OrderNumber,
                        Description = questionVM.Description,
                        Type = questionVM.Type,
                        IsActual = true
                    };
                    _context.Questions.Add(question);
                    await _context.SaveChangesAsync();

                    // Снапшот вопроса
                    var questionSnapshot = new QuestionsSnapshot
                    {
                        OlympSnapId = olympiadSnapshot.OlympSnapId,
                        QuestIdOriginal = question.QuestId,
                        QuestOrderSnapshot = question.QuestionOrder,
                        QuestionDescSnapshot = question.Description,
                        QuestionTypeSnapshot = question.Type
                    };
                    _context.QuestionsSnapshots.Add(questionSnapshot);
                    await _context.SaveChangesAsync();

                    // Обработка в зависимости от типа
                    if (questionVM.Type.StartsWith("auto"))
                    {
                        foreach (var opt in questionVM.Options.OrderBy(o => o.SortOrder))
                        {
                            var autoQuestion = new AutoQuestion
                            {
                                QuestId = question.QuestId,
                                OptionText = opt.OptionText,
                                IsCorrect = opt.IsCorrect,
                                SortOrder = opt.SortOrder
                            };
                            _context.AutoQuestions.Add(autoQuestion);

                            // Снапшот варианта
                            var autoSnapshot = new AutoQuestionsSnapshot
                            {
                                QuestSnapshotId = questionSnapshot.QuestSnapshotId,
                                OptionText = opt.OptionText,
                                IsCorrect = opt.IsCorrect,
                                SortOrder = opt.SortOrder
                            };
                            _context.AutoQuestionsSnapshots.Add(autoSnapshot);
                        }
                    }
                    else if (questionVM.Type == "manual")
                    {
                        var manualConfig = new ManualQuestionsConfig
                        {
                            QuestId = question.QuestId,
                            MaxScore = questionVM.MaxScore ?? 10,
                            ModelAnswer = questionVM.ModelAnswer
                        };
                        _context.ManualQuestionsConfigs.Add(manualConfig);

                        // Снапшот конфигурации
                        var manualSnapshot = new ManualQuestionsConfigSnapshot
                        {
                            QuestSnapshotId = questionSnapshot.QuestSnapshotId,
                            MaxScore = manualConfig.MaxScore,
                            ModelAnswer = manualConfig.ModelAnswer
                        };
                        _context.ManualQuestionsConfigSnapshots.Add(manualSnapshot);
                    }

                    // Вложения
                    foreach (var attachment in questionVM.Attachments.OrderBy(a => a.SortOrder))
                    {
                        var attachmentEntity = new QuestionAttachment
                        {
                            QuestId = question.QuestId,
                            ImageUrl = attachment.ImageUrl ?? string.Empty,
                            SortOrder = attachment.SortOrder
                        };
                        _context.QuestionAttachments.Add(attachmentEntity);

                        var attachmentSnapshot = new QuestionAttachmentsSnapshot
                        {
                            QuestSnapshotId = questionSnapshot.QuestSnapshotId,
                            ImageUrl = attachment.ImageUrl ?? string.Empty,
                            SortOrder = attachment.SortOrder
                        };
                        _context.QuestionAttachmentsSnapshots.Add(attachmentSnapshot);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return olympiad.OlympId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Ошибка при создании олимпиады: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateOlympiadAsync(CreateOlympiadViewModel model, int editorUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var olympiad = await _context.Olympiads
                    .FirstOrDefaultAsync(o => o.OlympId == model.OlympiadId);

                if (olympiad == null) return false;

                // Обновляем основные данные
                olympiad.Title = model.Title;
                olympiad.ImageUrl = model.ImageUrl;
                olympiad.Description = model.Description;
                olympiad.Credentials = model.Credentials;
                olympiad.EventStart = model.EventStart;
                olympiad.EventEnd = model.EventEnd;
                olympiad.RegistOpen = model.RegistOpen;
                olympiad.RegistClosed = model.RegistClosed;

                _context.Olympiads.Update(olympiad);

                // Обновляем снапшот
                var snapshot = await _context.OlympiadSnapshots
                    .FirstOrDefaultAsync(s => s.OriginalOlympId == model.OlympiadId);
                if (snapshot != null)
                {
                    snapshot.Title = model.Title;
                    snapshot.ImageUrl = model.ImageUrl;
                    snapshot.Description = model.Description;
                    snapshot.Credentials = model.Credentials;
                    snapshot.EventStart = model.EventStart;
                    snapshot.EventEnd = model.EventEnd;
                    snapshot.RegistOpen = model.RegistOpen;
                    snapshot.RegistClosed = model.RegistClosed;
                    snapshot.CreatedAtSnap = DateTime.Now;
                    _context.OlympiadSnapshots.Update(snapshot);
                }

                // Здесь нужно добавить логику обновления вопросов
                // (для простоты опущено, но должно быть реализовано)

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Ошибка при обновлении олимпиады: {ex.Message}", ex);
            }
        }

        public async Task<CreateOlympiadViewModel?> GetOlympiadForEditAsync(int olympiadId)
        {
            var olympiad = await _context.Olympiads
                .FirstOrDefaultAsync(o => o.OlympId == olympiadId);

            if (olympiad == null) return null;

            var questions = await _context.Questions
                .Where(q => q.OlympId == olympiadId && q.IsActual)
                .OrderBy(q => q.QuestionOrder)
                .ToListAsync();

            var model = new CreateOlympiadViewModel
            {
                OlympiadId = olympiad.OlympId,
                IsEditMode = true,
                Title = olympiad.Title,
                ImageUrl = olympiad.ImageUrl,
                Description = olympiad.Description ?? string.Empty,
                Credentials = olympiad.Credentials,
                RegistOpen = olympiad.RegistOpen,
                RegistClosed = olympiad.RegistClosed,
                EventStart = olympiad.EventStart,
                EventEnd = olympiad.EventEnd,
                Questions = new List<QuestionEditorViewModel>()
            };

            foreach (var question in questions.OrderBy(q => q.QuestionOrder))
            {
                var questionVM = new QuestionEditorViewModel
                {
                    TempId = question.QuestId,
                    QuestionId = question.QuestId,
                    OrderNumber = question.QuestionOrder,
                    Description = question.Description,
                    Type = question.Type,
                    IsExpanded = false,
                    Options = new List<AutoQuestionOptionEditorViewModel>(),
                    Attachments = new List<QuestionAttachmentEditorViewModel>()
                };

                if (question.Type.StartsWith("auto"))
                {
                    var options = await _context.AutoQuestions
                        .Where(o => o.QuestId == question.QuestId)
                        .OrderBy(o => o.SortOrder)
                        .ToListAsync();

                    foreach (var opt in options)
                    {
                        questionVM.Options.Add(new AutoQuestionOptionEditorViewModel
                        {
                            TempId = opt.QuestOptionId,
                            OptionId = opt.QuestOptionId,
                            OptionText = opt.OptionText,
                            IsCorrect = opt.IsCorrect,
                            SortOrder = opt.SortOrder
                        });
                    }
                }
                else if (question.Type == "manual")
                {
                    var config = await _context.ManualQuestionsConfigs
                        .FirstOrDefaultAsync(m => m.QuestId == question.QuestId);

                    if (config != null)
                    {
                        questionVM.MaxScore = config.MaxScore;
                        questionVM.ModelAnswer = config.ModelAnswer;
                    }
                }

                var attachments = await _context.QuestionAttachments
                    .Where(a => a.QuestId == question.QuestId)
                    .OrderBy(a => a.SortOrder)
                    .ToListAsync();

                foreach (var att in attachments)
                {
                    questionVM.Attachments.Add(new QuestionAttachmentEditorViewModel
                    {
                        TempId = att.AttachId,
                        AttachmentId = att.AttachId,
                        ImageUrl = att.ImageUrl,
                        SortOrder = att.SortOrder
                    });
                }

                model.Questions.Add(questionVM);
            }

            return model;
        }

        #endregion


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

            // Получаем название из снимка олимпиады через первый SubmissionItem
            var olympiadTitle = result.SubmissionItems
                .FirstOrDefault()?
                .QuestSnapshot?
                .OlympSnap?
                .Title ?? "Название не найдено";

            var viewModel = new ParticipantResultsViewModel
            {
                OlympiadId = result.Participant.OlympId,
                OlympiadTitle = olympiadTitle,  // Используем название из снимка
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


        #region Мои олимпиады

        /// <summary>
        /// Получение олимпиад участника с результатами
        /// </summary>
        public async Task<MyOlympiadsPagedResult<ParticipantOlympiadViewModel>> GetParticipantOlympiadsAsync(
            int userId,
            MyOlympiadsFilterViewModel? filter,
            int pageNumber,
            int pageSize = 6)
        {
            // Получаем все участия пользователя
            var query = _context.OlympiadParticipants
                .Include(p => p.Olymp)
                .Include(p => p.OlympiadResults)
                .Where(p => p.UserId == userId);

            // Применяем фильтры по олимпиаде
            if (filter != null)
            {
                // Фильтр по названию
                if (!string.IsNullOrWhiteSpace(filter.SearchTitle))
                {
                    query = query.Where(p => p.Olymp.Title.Contains(filter.SearchTitle));
                }

                // Фильтр по дате начала
                if (filter.StartDateFrom.HasValue)
                {
                    query = query.Where(p => p.Olymp.EventStart >= filter.StartDateFrom.Value);
                }
                if (filter.StartDateTo.HasValue)
                {
                    query = query.Where(p => p.Olymp.EventStart <= filter.StartDateTo.Value);
                }

                // Фильтр по дате окончания
                if (filter.EndDateFrom.HasValue)
                {
                    query = query.Where(p => p.Olymp.EventEnd >= filter.EndDateFrom.Value);
                }
                if (filter.EndDateTo.HasValue)
                {
                    query = query.Where(p => p.Olymp.EventEnd <= filter.EndDateTo.Value);
                }

                // Фильтр по статусу завершения
                if (filter.OnlyCompleted)
                {
                    query = query.Where(p => p.Status == "completed");
                }
                else if (filter.OnlyRegistered)
                {
                    query = query.Where(p => p.Status == "registered");
                }
            }

            // Сортировка: сначала активные (не завершённые), потом завершённые
            var orderedQuery = query.OrderBy(p => p.Status == "completed" ? 1 : 0)
                                   .ThenBy(p => p.Olymp.EventStart);

            var totalCount = await orderedQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (pageNumber < 1) pageNumber = 1;
            if (pageNumber > totalPages && totalPages > 0) pageNumber = totalPages;

            var items = await orderedQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ParticipantOlympiadViewModel
                {
                    OlympiadId = p.Olymp.OlympId,
                    Title = p.Olymp.Title,
                    ImageUrl = p.Olymp.ImageUrl ?? "/images/default-olympiad.jpg",
                    Description = p.Olymp.Description ?? string.Empty,
                    EventStart = p.Olymp.EventStart,
                    EventEnd = p.Olymp.EventEnd,
                    RegistOpen = p.Olymp.RegistOpen,
                    RegistClosed = p.Olymp.RegistClosed,
                    UserTotalScore = p.OlympiadResults.FirstOrDefault() != null
                        ? p.OlympiadResults.FirstOrDefault()!.TotalScore
                        : null,
                    MaxPossibleScore = 0, // Будет заполнено позже
                    IsCompleted = p.Status == "completed",
                    IsRegistered = p.Status == "registered",
                    CompletedAt = p.CompletedAt
                })
                .ToListAsync();

            // Заполняем MaxPossibleScore для каждого элемента
            foreach (var item in items)
            {
                item.MaxPossibleScore = await CalculateTotalMaxScoreForParticipantOlympiadAsync(item.OlympiadId);
            }

            return new MyOlympiadsPagedResult<ParticipantOlympiadViewModel>
            {
                Items = items,
                CurrentPage = pageNumber,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize,
                Filter = filter
            };
        }

        /// <summary>
        /// Получение максимального балла для олимпиады участника
        /// </summary>
        private async Task<int> CalculateTotalMaxScoreForParticipantOlympiadAsync(int olympiadId)
        {
            var questions = await _context.Questions
                .Where(q => q.OlympId == olympiadId && q.IsActual)
                .ToListAsync();

            int total = 0;
            foreach (var question in questions)
            {
                if (question.Type.StartsWith("auto"))
                {
                    total += 1; // auto вопросы дают 1 балл
                }
                else if (question.Type == "manual")
                {
                    var config = await _context.ManualQuestionsConfigs
                        .FirstOrDefaultAsync(m => m.QuestId == question.QuestId);
                    total += config?.MaxScore ?? 0;
                }
            }
            return total;
        }

        /// <summary>
        /// Получение олимпиад сотрудника (автор или проверяющий)
        /// </summary>
        public async Task<MyOlympiadsPagedResult<StaffOlympiadViewModel>> GetStaffOlympiadsAsync(
            int userId,
            MyOlympiadsFilterViewModel? filter,
            int pageNumber,
            int pageSize = 6)
        {
            // Получаем олимпиады, где пользователь является автором или проверяющим
            var query = _context.OlympStaffs
                .Include(s => s.Olymp)
                .Where(s => s.UserId == userId)
                .Select(s => new
                {
                    Staff = s,
                    Olympiad = s.Olymp,
                    UncheckedCount = _context.SubmissionItems
                        .Include(si => si.Results)
                            .ThenInclude(r => r.Participant)
                        .Include(si => si.QuestSnapshot)
                        .Where(si => si.QuestSnapshot.OlympSnap.OriginalOlympId == s.Olymp.OlympId
                            && si.Type == "manual"
                            && si.ManualSubmissionResult != null
                            && si.ManualSubmissionResult.ScoreValue == null)
                        .CountAsync().Result,
                    ParticipantsCount = _context.OlympiadParticipants
                        .Count(p => p.OlympId == s.Olymp.OlympId)
                });

            // Применяем фильтры
            if (filter != null)
            {
                if (!string.IsNullOrWhiteSpace(filter.SearchTitle))
                {
                    query = query.Where(s => s.Olympiad.Title.Contains(filter.SearchTitle));
                }
                if (filter.StartDateFrom.HasValue)
                {
                    query = query.Where(s => s.Olympiad.EventStart >= filter.StartDateFrom.Value);
                }
                if (filter.StartDateTo.HasValue)
                {
                    query = query.Where(s => s.Olympiad.EventStart <= filter.StartDateTo.Value);
                }
                if (filter.EndDateFrom.HasValue)
                {
                    query = query.Where(s => s.Olympiad.EventEnd >= filter.EndDateFrom.Value);
                }
                if (filter.EndDateTo.HasValue)
                {
                    query = query.Where(s => s.Olympiad.EventEnd <= filter.EndDateTo.Value);
                }
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (pageNumber < 1) pageNumber = 1;
            if (pageNumber > totalPages && totalPages > 0) pageNumber = totalPages;

            var itemsData = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    s.Olympiad,
                    s.Staff.OlimpRole,
                    s.UncheckedCount,
                    s.ParticipantsCount
                })
                .ToListAsync();

            var items = itemsData.Select(s => new StaffOlympiadViewModel
            {
                OlympiadId = s.Olympiad.OlympId,
                Title = s.Olympiad.Title,
                ImageUrl = s.Olympiad.ImageUrl ?? "/images/default-olympiad.jpg",
                Description = s.Olympiad.Description ?? string.Empty,
                EventStart = s.Olympiad.EventStart,
                EventEnd = s.Olympiad.EventEnd,
                RegistOpen = s.Olympiad.RegistOpen,
                RegistClosed = s.Olympiad.RegistClosed,
                IsAuthor = s.OlimpRole == "author",
                IsReviewer = s.OlimpRole == "reviewer" || s.OlimpRole == "moderator",
                UncheckedManualAnswers = s.UncheckedCount,
                TotalParticipants = s.ParticipantsCount
            }).ToList();

            return new MyOlympiadsPagedResult<StaffOlympiadViewModel>
            {
                Items = items,
                CurrentPage = pageNumber,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize,
                Filter = filter
            };
        }

        /// <summary>
        /// Получение всех олимпиад для администратора
        /// </summary>
        public async Task<MyOlympiadsPagedResult<AdminOlympiadViewModel>> GetAllOlympiadsForAdminAsync(
            MyOlympiadsFilterViewModel? filter,
            int pageNumber,
            int pageSize = 10)
        {
            var query = _context.Olympiads.AsQueryable();

            // Применяем фильтры
            if (filter != null)
            {
                if (!string.IsNullOrWhiteSpace(filter.SearchTitle))
                {
                    query = query.Where(o => o.Title.Contains(filter.SearchTitle));
                }
                if (filter.StartDateFrom.HasValue)
                {
                    query = query.Where(o => o.EventStart >= filter.StartDateFrom.Value);
                }
                if (filter.StartDateTo.HasValue)
                {
                    query = query.Where(o => o.EventStart <= filter.StartDateTo.Value);
                }
                if (filter.EndDateFrom.HasValue)
                {
                    query = query.Where(o => o.EventEnd >= filter.EndDateFrom.Value);
                }
                if (filter.EndDateTo.HasValue)
                {
                    query = query.Where(o => o.EventEnd <= filter.EndDateTo.Value);
                }
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (pageNumber < 1) pageNumber = 1;
            if (pageNumber > totalPages && totalPages > 0) pageNumber = totalPages;

            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new AdminOlympiadViewModel
                {
                    OlympiadId = o.OlympId,
                    Title = o.Title,
                    ImageUrl = o.ImageUrl ?? string.Empty,
                    Description = o.Description ?? string.Empty,
                    Credentials = o.Credentials ?? string.Empty,
                    Status = o.Status,
                    EventStart = o.EventStart,
                    EventEnd = o.EventEnd,
                    RegistOpen = o.RegistOpen,
                    RegistClosed = o.RegistClosed,
                    CreatedAt = o.CreatedAt,
                    //QuestionsCount = _context.Questions.Count(q => q.OlympId == o.OlympId && q.IsActual),
                    //ParticipantsCount = _context.OlympiadParticipants.Count(p => p.OlympId == o.OlympId)
                })
                .ToListAsync();

            return new MyOlympiadsPagedResult<AdminOlympiadViewModel>
            {
                Items = items,
                CurrentPage = pageNumber,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize,
                Filter = filter
            };
        }

        #endregion

    }
}
