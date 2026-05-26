using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Olimpiadnic.Models.RoleModels;
using Olimpiadnic.Services.Repos;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Olimpiadnic.Controllers
{
    [Authorize(Roles = "staff,admin")]
    public class StaffBoardController : Controller
    {
        private readonly ILogger<StaffBoardController> _logger;
        private readonly IOlympiadRepository _olympiadRepository;
        private readonly IOlympiadEditorSessionService _sessionService;

        public StaffBoardController(
            ILogger<StaffBoardController> logger,
            IOlympiadRepository olympiadRepository,
            IOlympiadEditorSessionService sessionService)
        {
            _logger = logger;
            _olympiadRepository = olympiadRepository;
            _sessionService = sessionService;
        }

        private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        #region Основные страницы

        [HttpGet]
        public async Task<IActionResult> Create(int? id)
        {
            var userId = GetCurrentUserId();
            CreateOlympiadViewModel model;

            if (id.HasValue && id.Value > 0)
            {
                // Режим редактирования - загружаем из БД
                model = await _olympiadRepository.GetOlympiadForEditAsync(id.Value);
                if (model == null) return NotFound();
                model.IsEditMode = true;

                // Сохраняем в сессию
                await _sessionService.SaveSessionAsync(userId, model);
            }
            else
            {
                // Проверяем существующую сессию
                var existingSession = await _sessionService.GetSessionAsync(userId);
                if (existingSession != null)
                {
                    model = existingSession;
                    TempData["Info"] = "Восстановлен черновик";
                }
                else
                {
                    // Создаём новую сессию с одним вопросом
                    model = new CreateOlympiadViewModel
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
                            CreateEmptyQuestion(1, 1)
                        }
                    };
                    await _sessionService.SaveSessionAsync(userId, model);
                }
            }

            // Передаём только текущий вопрос во View
            var viewModel = new CreateOlympiadViewModel
            {
                OlympiadId = model.OlympiadId,
                IsEditMode = model.IsEditMode,
                Title = model.Title,
                ImageUrl = model.ImageUrl,
                Description = model.Description,
                Credentials = model.Credentials,
                RegistOpen = model.RegistOpen,
                RegistClosed = model.RegistClosed,
                EventStart = model.EventStart,
                EventEnd = model.EventEnd,
                CurrentQuestionIndex = model.CurrentQuestionIndex,
                Questions = model.Questions.Count > 0
                    ? new List<QuestionEditorViewModel> { model.Questions[model.CurrentQuestionIndex] }
                    : new List<QuestionEditorViewModel>()
            };

            return View(viewModel);
        }

        private QuestionEditorViewModel CreateEmptyQuestion(int tempId, int orderNumber)
        {
            return new QuestionEditorViewModel
            {
                TempId = tempId,
                OrderNumber = orderNumber,
                Type = "manual",
                IsExpanded = true,
                Description = string.Empty,
                MaxScore = 10,
                ModelAnswer = string.Empty,
                Options = new List<AutoQuestionOptionEditorViewModel>(),
                Attachments = new List<QuestionAttachmentEditorViewModel>()
            };
        }

        #endregion

        #region API для управления вопросами

        // Получить навигационные данные (список всех вопросов)
        [HttpGet]
        public async Task<IActionResult> GetNavigationData()
        {
            var userId = GetCurrentUserId();
            var session = await _sessionService.GetSessionAsync(userId);

            if (session == null)
                return Json(new { success = false });

            var questionsData = session.Questions.Select((q, idx) => new
            {
                id = q.TempId,
                number = q.OrderNumber,
                isSaved = !string.IsNullOrWhiteSpace(q.Description) && q.Description.Length > 10,
                isCurrent = idx == session.CurrentQuestionIndex,
                shortTitle = q.ShortTitle
            }).ToList();

            return Json(new
            {
                success = true,
                currentIndex = session.CurrentQuestionIndex,
                questions = questionsData,
                totalQuestions = session.Questions.Count
            });
        }

        // Получить конкретный вопрос для отображения
        [HttpGet]
        public async Task<IActionResult> GetQuestion(int index)
        {
            var userId = GetCurrentUserId();
            var session = await _sessionService.GetSessionAsync(userId);

            if (session == null || index < 0 || index >= session.Questions.Count)
                return Json(new { success = false, message = "Вопрос не найден" });

            // Обновляем текущий индекс
            session.CurrentQuestionIndex = index;
            await _sessionService.SaveSessionAsync(userId, session);

            return PartialView("_QuestionEditor", session.Questions[index]);
        }

        #region Дебаг методы
        [HttpGet]
        public async Task<IActionResult> GetSessionDebug()
        {
            var userId = GetCurrentUserId();
            var session = await _sessionService.GetSessionAsync(userId);

            if (session == null)
                return Json(new { exists = false });

            return Json(new
            {
                exists = true,
                questionsCount = session.Questions.Count,
                questions = session.Questions.Select(q => new
                {
                    q.TempId,
                    q.OrderNumber,
                    q.Description,
                    q.Type,
                    optionsCount = q.Options.Count
                })
            });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DebugPost()
        {
            using var reader = new StreamReader(Request.Body);
            var rawBody = await reader.ReadToEndAsync();

            _logger.LogInformation($"Raw body: {rawBody}");

            return Json(new { raw = rawBody });
        }
        #endregion

        //Сохранить текущий вопрос
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SaveCurrentQuestion()
        {
            try
            {
                // Читаем raw body
                using var reader = new StreamReader(Request.Body);
                var rawBody = await reader.ReadToEndAsync();

                if (string.IsNullOrEmpty(rawBody))
                {
                    return Json(new { success = false, message = "Empty body" });
                }

                _logger.LogInformation($"Raw body: {rawBody}");

                // Десериализуем вручную
                using var doc = JsonDocument.Parse(rawBody);
                var root = doc.RootElement;

                // Проверяем наличие всех полей
                if (!root.TryGetProperty("tempId", out var tempIdProp))
                    return Json(new { success = false, message = "Missing tempId" });

                var tempId = tempIdProp.GetInt32();

                if (!root.TryGetProperty("description", out var descProp))
                    return Json(new { success = false, message = "Missing description" });
                var description = descProp.GetString() ?? "";

                if (!root.TryGetProperty("type", out var typeProp))
                    return Json(new { success = false, message = "Missing type" });
                var type = typeProp.GetString() ?? "manual";

                _logger.LogInformation($"Parsed: tempId={tempId}, description={description}, type={type}");

                var userId = GetCurrentUserId();
                var session = await _sessionService.GetSessionAsync(userId);

                if (session == null)
                {
                    return Json(new { success = false, message = "Сессия не найдена" });
                }

                var questionIndex = session.Questions.FindIndex(q => q.TempId == tempId);
                if (questionIndex == -1)
                {
                    return Json(new { success = false, message = $"Вопрос с TempId={tempId} не найден" });
                }

                var existingQuestion = session.Questions[questionIndex];
                existingQuestion.Description = description;
                existingQuestion.Type = type;

                if (type == "manual")
                {
                    if (root.TryGetProperty("maxScore", out var maxScoreProp))
                        existingQuestion.MaxScore = maxScoreProp.GetInt32();
                    if (root.TryGetProperty("modelAnswer", out var modelAnswerProp))
                        existingQuestion.ModelAnswer = modelAnswerProp.GetString();
                    existingQuestion.Options = new List<AutoQuestionOptionEditorViewModel>();
                }
                else if (type.StartsWith("auto"))
                {
                    existingQuestion.MaxScore = null;
                    existingQuestion.ModelAnswer = null;

                    var options = new List<AutoQuestionOptionEditorViewModel>();

                    // ВАЖНО: Проверяем наличие поля options
                    if (root.TryGetProperty("options", out var optionsProp))
                    {
                        _logger.LogInformation($"Options found, array length: {optionsProp.GetArrayLength()}");

                        foreach (var opt in optionsProp.EnumerateArray())
                        {
                            try
                            {
                                // С проверкой каждого поля
                                if (!opt.TryGetProperty("tempId", out var optTempIdProp))
                                {
                                    _logger.LogWarning("Option missing tempId");
                                    continue;
                                }

                                if (!opt.TryGetProperty("optionText", out var optTextProp))
                                {
                                    _logger.LogWarning("Option missing optionText");
                                    continue;
                                }

                                if (!opt.TryGetProperty("isCorrect", out var isCorrectProp))
                                {
                                    _logger.LogWarning("Option missing isCorrect");
                                    continue;
                                }

                                if (!opt.TryGetProperty("sortOrder", out var sortOrderProp))
                                {
                                    _logger.LogWarning("Option missing sortOrder");
                                    continue;
                                }

                                options.Add(new AutoQuestionOptionEditorViewModel
                                {
                                    TempId = optTempIdProp.GetInt32(),
                                    OptionText = optTextProp.GetString() ?? "",
                                    IsCorrect = isCorrectProp.GetBoolean(),
                                    SortOrder = sortOrderProp.GetInt32()
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error parsing option");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogInformation("No options property found");
                    }

                    existingQuestion.Options = options;
                    _logger.LogInformation($"Saved {options.Count} options");
                }

                if (root.TryGetProperty("attachments", out var attachmentsProp))
                {
                    var attachments = new List<QuestionAttachmentEditorViewModel>();
                    foreach (var att in attachmentsProp.EnumerateArray())
                    {
                        attachments.Add(new QuestionAttachmentEditorViewModel
                        {
                            TempId = att.GetProperty("tempId").GetInt32(),
                            ImageUrl = att.GetProperty("imageUrl").GetString(),
                            SortOrder = att.GetProperty("sortOrder").GetInt32()
                        });
                    }
                    existingQuestion.Attachments = attachments;
                }

                await _sessionService.SaveSessionAsync(userId, session);

                _logger.LogInformation($"Question {tempId} saved successfully");

                return Json(new { success = true, message = "Вопрос сохранён" });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error");
                return Json(new { success = false, message = $"JSON ошибка: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveCurrentQuestion");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Добавить новый вопрос
        [HttpPost]
        public async Task<IActionResult> AddQuestion()
        {
            var userId = GetCurrentUserId();
            var newTempId = await _sessionService.AddQuestionAsync(userId);

            var session = await _sessionService.GetSessionAsync(userId);
            var newQuestion = session?.Questions.LastOrDefault();

            return PartialView("_QuestionEditor", newQuestion);
        }

        // Удалить вопрос
        [HttpPost]
        public async Task<IActionResult> RemoveQuestion([FromBody] RemoveQuestionRequest request)
        {
            var userId = GetCurrentUserId();
            var session = await _sessionService.GetSessionAsync(userId);

            if (session == null)
                return Json(new { success = false, message = "Сессия не найдена" });

            var questionIndex = session.Questions.FindIndex(q => q.TempId == request.TempId);
            if (questionIndex == -1)
                return Json(new { success = false, message = "Вопрос не найден" });

            await _sessionService.RemoveQuestionAsync(userId, questionIndex);

            return Json(new { success = true });
        }

        // Переместить вопрос (drag & drop)
        [HttpPost]
        public async Task<IActionResult> ReorderQuestions([FromBody] ReorderRequest request)
        {
            var userId = GetCurrentUserId();
            await _sessionService.ReorderQuestionsAsync(userId, request.QuestionOrder);

            return Json(new { success = true });
        }

        // Сохранить основную информацию об олимпиаде
        [HttpPost]
        public async Task<IActionResult> SaveOlympiadInfo([FromBody] OlympiadInfoRequest request)
        {
            var userId = GetCurrentUserId();
            var session = await _sessionService.GetSessionAsync(userId);

            if (session == null)
                return Json(new { success = false, message = "Сессия не найдена" });

            session.Title = request.Title;
            session.Description = request.Description;
            session.Credentials = request.Credentials;
            session.ImageUrl = request.ImageUrl;
            session.RegistOpen = request.RegistOpen;
            session.RegistClosed = request.RegistClosed;
            session.EventStart = request.EventStart;
            session.EventEnd = request.EventEnd;

            await _sessionService.SaveSessionAsync(userId, session);

            return Json(new { success = true });
        }

        // Публикация олимпиады
        [HttpPost]
        public async Task<IActionResult> PublishOlympiad()
        {
            var userId = GetCurrentUserId();
            var session = await _sessionService.GetSessionAsync(userId);

            if (session == null)
                return Json(new { success = false, message = "Нет данных для сохранения" });

            // Валидация
            if (string.IsNullOrWhiteSpace(session.Title))
                return Json(new { success = false, message = "Введите название олимпиады" });

            if (string.IsNullOrWhiteSpace(session.Description))
                return Json(new { success = false, message = "Введите описание олимпиады" });

            if (!session.Questions.Any())
                return Json(new { success = false, message = "Добавьте хотя бы один вопрос" });

            foreach (var q in session.Questions)
            {
                if (string.IsNullOrWhiteSpace(q.Description) || q.Description.Length < 5)
                    return Json(new { success = false, message = $"Вопрос {q.OrderNumber}: введите текст вопроса (минимум 5 символов)" });

                if (q.Type == "manual")
                {
                    if (!q.MaxScore.HasValue || q.MaxScore < 1)
                        return Json(new { success = false, message = $"Вопрос {q.OrderNumber}: укажите максимальный балл" });
                }
                else if (q.Type.StartsWith("auto"))
                {
                    if (q.Options.Count < 2)
                        return Json(new { success = false, message = $"Вопрос {q.OrderNumber}: добавьте минимум 2 варианта ответа" });

                    if (!q.Options.Any(o => o.IsCorrect))
                        return Json(new { success = false, message = $"Вопрос {q.OrderNumber}: отметьте хотя бы один правильный ответ" });
                }
            }

            try
            {
                int olympiadId;

                if (session.IsEditMode && session.OlympiadId > 0)
                {
                    var result = await _olympiadRepository.UpdateOlympiadAsync(session, userId);
                    olympiadId = session.OlympiadId;
                }
                else
                {
                    olympiadId = await _olympiadRepository.CreateOlympiadAsync(session, userId);
                }

                // Очищаем сессию
                await _sessionService.DeleteSessionAsync(userId);

                return Json(new { success = true, olympiadId, message = "Олимпиада успешно сохранена!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сохранении олимпиады");
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        #endregion
    }

    #region Модели запросов

    public class SaveQuestionRequest
    {
        [JsonPropertyName("tempId")]
        public int TempId { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "manual";

        [JsonPropertyName("maxScore")]
        public int? MaxScore { get; set; }

        [JsonPropertyName("modelAnswer")]
        public string? ModelAnswer { get; set; }

        [JsonPropertyName("options")]
        public List<OptionDto>? Options { get; set; }

        [JsonPropertyName("attachments")]
        public List<AttachmentDto>? Attachments { get; set; }
    }

    // ВАЖНО: имена свойств ДОЛЖНЫ СОВПАДАТЬ с тем, что приходит из JS
    public class OptionDto
    {
        [JsonPropertyName("tempId")]
        public int TempId { get; set; }

        [JsonPropertyName("optionText")]
        public string OptionText { get; set; } = string.Empty;

        [JsonPropertyName("isCorrect")]
        public bool IsCorrect { get; set; }

        [JsonPropertyName("sortOrder")]
        public int SortOrder { get; set; }
    }

    public class AttachmentDto
    {
        [JsonPropertyName("tempId")]
        public int TempId { get; set; }

        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("sortOrder")]
        public int SortOrder { get; set; }
    }

    public class RemoveQuestionRequest
    {
        public int TempId { get; set; }
    }

    public class ReorderRequest
    {
        public List<int> QuestionOrder { get; set; } = new();
    }

    public class OlympiadInfoRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Credentials { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime RegistOpen { get; set; }
        public DateTime RegistClosed { get; set; }
        public DateTime EventStart { get; set; }
        public DateTime EventEnd { get; set; }
    }

    #endregion
}