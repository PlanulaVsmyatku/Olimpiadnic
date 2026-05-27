// Api/OlympiadEditorApiController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Olimpiadnic.Data;
using Olimpiadnic.Models.RoleModels;
using Olimpiadnic.Services.Repos;
using Olimpiadnic.Services.Session;
using System.Security.Claims;

namespace Olimpiadnic.Controllers.Api
{
    [Authorize(Roles = "staff,admin")]
    [ApiController]
    [Route("api/editor")]
    public class OlympiadEditorApiController : ControllerBase
    {
        private readonly IOlympiadEditorSessionService _sessionService;
        private readonly IOlympiadRepository _olympiadRepository;
        private readonly ILogger<OlympiadEditorApiController> _logger;

        public OlympiadEditorApiController(
            IOlympiadEditorSessionService sessionService,
            ILogger<OlympiadEditorApiController> logger,
            IOlympiadRepository repository)
        {
            _sessionService = sessionService;
            _olympiadRepository = repository;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        #region Основная информация

        /// <summary>
        /// Получение текущего черновика
        /// </summary>
        [HttpGet("session")]
        public async Task<IActionResult> GetSession()
        {
            var userId = GetCurrentUserId();
            var session = await _sessionService.GetSessionAsync(userId);
            
            if (session == null)
            {
                return Ok(new { success = true, hasSession = false });
            }
            
            return Ok(new
            {
                success = true,
                hasSession = true,
                olympiad = new
                {
                    session.OlympiadId,
                    session.IsEditMode,
                    session.Title,
                    session.Description,
                    session.Credentials,
                    session.ImageUrl,
                    RegistOpen = session.RegistOpen.ToString("yyyy-MM-ddTHH:mm"),
                    RegistClosed = session.RegistClosed.ToString("yyyy-MM-ddTHH:mm"),
                    EventStart = session.EventStart.ToString("yyyy-MM-ddTHH:mm"),
                    EventEnd = session.EventEnd.ToString("yyyy-MM-ddTHH:mm")
                },
                questions = session.Questions.Select((q, idx) => new
                {
                    index = idx,
                    q.TempId,
                    q.QuestionId,
                    q.OrderNumber,
                    q.Description,
                    q.Type,
                    q.IsSaved,
                    ShortTitle = q.ShortTitle,
                    Options = q.Options.Select(o => new
                    {
                        o.TempId,
                        o.OptionId,
                        o.OptionText,
                        o.IsCorrect,
                        o.SortOrder
                    }),
                    Attachments = q.Attachments.Select(a => new
                    {
                        a.TempId,
                        a.AttachmentId,
                        a.ImageUrl,
                        a.SortOrder
                    }),
                    q.MaxScore,
                    q.ModelAnswer
                }),
                currentQuestionIndex = session.CurrentQuestionIndex
            });
        }

        /// <summary>
        /// Сохранение основной информации
        /// </summary>
        [HttpPost("save-info")]
        public async Task<IActionResult> SaveOlympiadInfo([FromBody] SaveOlympiadInfoRequest request)
        {
            var userId = GetCurrentUserId();
            
            var olympiadInfo = new CreateOlympiadViewModel
            {
                Title = request.Title ?? string.Empty,
                Description = request.Description ?? string.Empty,
                Credentials = request.Credentials,
                ImageUrl = request.ImageUrl,
                RegistOpen = request.RegistOpen,
                RegistClosed = request.RegistClosed,
                EventStart = request.EventStart,
                EventEnd = request.EventEnd
            };
            
            await _sessionService.UpdateOlympiadInfoAsync(userId, olympiadInfo);
            
            return Ok(new { success = true, message = "Информация сохранена" });
        }

        #endregion

        #region Вопросы

        /// <summary>
        /// Получение конкретного вопроса
        /// </summary>
        [HttpGet("question/{index}")]
        public async Task<IActionResult> GetQuestion(int index)
        {
            var userId = GetCurrentUserId();
            var question = await _sessionService.GetQuestionAsync(userId, index);
            
            if (question == null)
            {
                return BadRequest(new { success = false, message = "Вопрос не найден" });
            }
            
            return Ok(new
            {
                success = true,
                question = new
                {
                    question.TempId,
                    question.QuestionId,
                    question.OrderNumber,
                    question.Description,
                    question.Type,
                    Options = question.Options.Select(o => new
                    {
                        o.TempId,
                        o.OptionId,
                        o.OptionText,
                        o.IsCorrect,
                        o.SortOrder
                    }),
                    Attachments = question.Attachments.Select(a => new
                    {
                        a.TempId,
                        a.AttachmentId,
                        a.ImageUrl,
                        a.SortOrder
                    }),
                    question.MaxScore,
                    question.ModelAnswer
                }
            });
        }

        /// <summary>
        /// Сохранение вопроса (полное обновление)
        /// </summary>
        [HttpPost("save-question")]
        public async Task<IActionResult> SaveQuestion([FromBody] SaveQuestionRequest request)
        {
            var userId = GetCurrentUserId();
            
            // Получаем существующий вопрос, чтобы сохранить неизменяемые поля
            var existingQuestion = await _sessionService.GetQuestionAsync(userId, request.Index);
            
            var question = new QuestionEditorViewModel
            {
                TempId = existingQuestion?.TempId ?? request.TempId,
                QuestionId = existingQuestion?.QuestionId ?? request.QuestionId,
                OrderNumber = request.OrderNumber,
                Description = request.Description ?? string.Empty,
                Type = request.Type,
                IsActual = true,
                IsExpanded = true,
                Options = request.Options?.Select((o, idx) => new AutoQuestionOptionEditorViewModel
                {
                    TempId = o.TempId > 0 ? o.TempId : GetNextTempIdClient(),
                    OptionId = o.OptionId,
                    OptionText = o.OptionText,
                    IsCorrect = o.IsCorrect,
                    SortOrder = idx + 1
                }).ToList() ?? new List<AutoQuestionOptionEditorViewModel>(),
                MaxScore = request.Type == "manual" ? request.MaxScore : null,
                ModelAnswer = request.Type == "manual" ? request.ModelAnswer : null,
                Attachments = request.Attachments?.Select((a, idx) => new QuestionAttachmentEditorViewModel
                {
                    TempId = a.TempId > 0 ? a.TempId : GetNextTempIdClient(),
                    AttachmentId = a.AttachmentId,
                    ImageUrl = a.ImageUrl,
                    SortOrder = idx + 1
                }).ToList() ?? new List<QuestionAttachmentEditorViewModel>()
            };
            
            await _sessionService.UpdateQuestionAsync(userId, request.Index, question);
            
            return Ok(new { success = true, message = "Вопрос сохранен" });
        }

        private int _nextClientTempId = 10000;
        private int GetNextTempIdClient()
        {
            return _nextClientTempId++;
        }

        /// <summary>
        /// Добавление нового вопроса
        /// </summary>
        [HttpPost("add-question")]
        public async Task<IActionResult> AddQuestion()
        {
            var userId = GetCurrentUserId();
            var newIndex = await _sessionService.AddQuestionAsync(userId);
            
            var session = await _sessionService.GetSessionAsync(userId);
            var newQuestion = session?.Questions[newIndex];
            
            return Ok(new
            {
                success = true,
                index = newIndex,
                question = new
                {
                    newQuestion?.TempId,
                    newQuestion?.OrderNumber,
                    newQuestion?.Description,
                    newQuestion?.Type,
                    Options = newQuestion?.Options ?? new List<AutoQuestionOptionEditorViewModel>(),
                    Attachments = newQuestion?.Attachments ?? new List<QuestionAttachmentEditorViewModel>(),
                    newQuestion?.MaxScore,
                    newQuestion?.ModelAnswer
                }
            });
        }

        /// <summary>
        /// Удаление вопроса
        /// </summary>
        [HttpPost("remove-question")]
        public async Task<IActionResult> RemoveQuestion([FromBody] RemoveQuestionRequest request)
        {
            var userId = GetCurrentUserId();
            await _sessionService.RemoveQuestionAsync(userId, request.Index);
            
            var session = await _sessionService.GetSessionAsync(userId);
            var updatedQuestions = session?.Questions.Select((q, idx) => new
            {
                index = idx,
                q.TempId,
                q.OrderNumber,
                ShortTitle = q.ShortTitle
            }).ToList();
            
            return Ok(new { success = true, questions = updatedQuestions });
        }

        /// <summary>
        /// Перестановка вопросов
        /// </summary>
        [HttpPost("reorder-questions")]
        public async Task<IActionResult> ReorderQuestions([FromBody] ReorderQuestionsRequest request)
        {
            var userId = GetCurrentUserId();
            await _sessionService.ReorderQuestionsAsync(userId, request.NewOrder);
            
            return Ok(new { success = true });
        }

        #endregion

        #region Опции вопроса (для auto-вопросов)

        /// <summary>
        /// Добавление варианта ответа
        /// </summary>
        [HttpPost("add-option")]
        public async Task<IActionResult> AddOption([FromBody] AddOptionRequest request)
        {
            var userId = GetCurrentUserId();
            var question = await _sessionService.GetQuestionAsync(userId, request.QuestionIndex);
            
            if (question == null)
            {
                return BadRequest(new { success = false, message = "Вопрос не найден" });
            }
            
            var newOption = new AutoQuestionOptionEditorViewModel
            {
                TempId = GetNextTempIdClient(),
                OptionId = null,
                OptionText = "",
                IsCorrect = false,
                SortOrder = question.Options.Count + 1
            };
            
            question.Options.Add(newOption);
            await _sessionService.UpdateQuestionAsync(userId, request.QuestionIndex, question);
            
            return Ok(new
            {
                success = true,
                option = new
                {
                    newOption.TempId,
                    newOption.OptionText,
                    newOption.IsCorrect,
                    newOption.SortOrder
                }
            });
        }

        /// <summary>
        /// Обновление варианта ответа
        /// </summary>
        [HttpPost("update-option")]
        public async Task<IActionResult> UpdateOption([FromBody] UpdateOptionRequest request)
        {
            var userId = GetCurrentUserId();
            var question = await _sessionService.GetQuestionAsync(userId, request.QuestionIndex);
            
            if (question == null || request.OptionIndex < 0 || request.OptionIndex >= question.Options.Count)
            {
                return BadRequest(new { success = false, message = "Вариант ответа не найден" });
            }
            
            var option = question.Options[request.OptionIndex];
            option.OptionText = request.OptionText;
            option.IsCorrect = request.IsCorrect;
            
            await _sessionService.UpdateQuestionAsync(userId, request.QuestionIndex, question);
            
            return Ok(new { success = true });
        }

        /// <summary>
        /// Удаление варианта ответа
        /// </summary>
        [HttpPost("remove-option")]
        public async Task<IActionResult> RemoveOption([FromBody] RemoveOptionRequest request)
        {
            var userId = GetCurrentUserId();
            await _sessionService.RemoveQuestionOptionAsync(userId, request.QuestionIndex, request.OptionIndex);
            
            return Ok(new { success = true });
        }

        #endregion

        #region Вложения

        /// <summary>
        /// Добавление вложения
        /// </summary>
        [HttpPost("add-attachment")]
        public async Task<IActionResult> AddAttachment([FromBody] AddAttachmentRequest request)
        {
            var userId = GetCurrentUserId();
            await _sessionService.AddAttachmentAsync(userId, request.QuestionIndex, request.ImageUrl);
            
            var question = await _sessionService.GetQuestionAsync(userId, request.QuestionIndex);
            var newAttachment = question?.Attachments.LastOrDefault();
            
            return Ok(new
            {
                success = true,
                attachment = new
                {
                    newAttachment?.TempId,
                    newAttachment?.ImageUrl,
                    newAttachment?.SortOrder
                }
            });
        }

        /// <summary>
        /// Удаление вложения
        /// </summary>
        [HttpPost("remove-attachment")]
        public async Task<IActionResult> RemoveAttachment([FromBody] RemoveAttachmentRequest request)
        {
            var userId = GetCurrentUserId();
            await _sessionService.RemoveAttachmentAsync(userId, request.QuestionIndex, request.AttachmentIndex);
            
            return Ok(new { success = true });
        }

        /// <summary>
        /// Публикация олимпиады (сохранение в БД)
        /// </summary>
        [HttpPost("publish")]
        public async Task<IActionResult> PublishOlympiad()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(new { success = false, message = "Пользователь не авторизован" });

            // Получаем черновик из сессии
            var draft = await _sessionService.GetSessionAsync(userId);
            if (draft == null)
                return BadRequest(new { success = false, message = "Нет данных для публикации" });

            // Валидация
            if (string.IsNullOrWhiteSpace(draft.Title))
                return BadRequest(new { success = false, message = "Укажите название олимпиады" });

            if (string.IsNullOrWhiteSpace(draft.Description))
                return BadRequest(new { success = false, message = "Укажите описание олимпиады" });

            if (draft.Questions == null || !draft.Questions.Any())
                return BadRequest(new { success = false, message = "Добавьте хотя бы один вопрос" });

            // Проверяем каждый вопрос на минимальную заполненность
            foreach (var question in draft.Questions)
            {
                if (string.IsNullOrWhiteSpace(question.Description))
                    return BadRequest(new { success = false, message = $"Вопрос {question.OrderNumber}: укажите текст вопроса" });

                if (question.Type.StartsWith("auto"))
                {
                    if (question.Options == null || !question.Options.Any())
                        return BadRequest(new { success = false, message = $"Вопрос {question.OrderNumber}: добавьте варианты ответов" });

                    // Проверяем, что есть хотя бы один правильный вариант
                    if (!question.Options.Any(o => o.IsCorrect))
                        return BadRequest(new { success = false, message = $"Вопрос {question.OrderNumber}: отметьте хотя бы один правильный вариант" });
                }
                else if (question.Type == "manual")
                {
                    if (question.MaxScore == null || question.MaxScore <= 0)
                        return BadRequest(new { success = false, message = $"Вопрос {question.OrderNumber}: укажите максимальный балл" });
                }
            }

            try
            {
                int olympiadId;

                if (draft.IsEditMode && draft.OlympiadId > 0)
                {
                    olympiadId = draft.OlympiadId;

                    // Обновление существующей олимпиады
                    var success = await _olympiadRepository.UpdateOlympiadAsync(draft, userId);
                    if (!success)
                        return BadRequest(new { success = false, message = "Ошибка при обновлении олимпиады" });

                }
                else
                {
                    // Создание новой олимпиады
                    olympiadId = await _olympiadRepository.CreateOlympiadAsync(draft, userId);
                }

                // Очищаем сессию после успешной публикации
                await _sessionService.DeleteSessionAsync(userId);

                return Ok(new
                {
                    success = true,
                    olympiadId = olympiadId,
                    message = draft.IsEditMode ? "Олимпиада успешно обновлена" : "Олимпиада успешно создана"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при публикации олимпиады");
                return StatusCode(500, new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        #endregion

        #region Request Models

        public class SaveOlympiadInfoRequest
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

        public class SaveQuestionRequest
        {
            public int Index { get; set; }
            public int TempId { get; set; }
            public int? QuestionId { get; set; }
            public int OrderNumber { get; set; }
            public string Description { get; set; } = string.Empty;
            public string Type { get; set; } = "manual";
            public List<SaveOptionRequest>? Options { get; set; }
            public int? MaxScore { get; set; }
            public string? ModelAnswer { get; set; }
            public List<SaveAttachmentRequest>? Attachments { get; set; }
        }

        public class SaveOptionRequest
        {
            public int TempId { get; set; }
            public int? OptionId { get; set; }
            public string OptionText { get; set; } = string.Empty;
            public bool IsCorrect { get; set; }
        }

        public class SaveAttachmentRequest
        {
            public int TempId { get; set; }
            public int? AttachmentId { get; set; }
            public string? ImageUrl { get; set; }
        }

        public class RemoveQuestionRequest
        {
            public int Index { get; set; }
        }

        public class ReorderQuestionsRequest
        {
            public List<int> NewOrder { get; set; } = new();
        }

        public class AddOptionRequest
        {
            public int QuestionIndex { get; set; }
        }

        public class UpdateOptionRequest
        {
            public int QuestionIndex { get; set; }
            public int OptionIndex { get; set; }
            public string OptionText { get; set; } = string.Empty;
            public bool IsCorrect { get; set; }
        }

        public class RemoveOptionRequest
        {
            public int QuestionIndex { get; set; }
            public int OptionIndex { get; set; }
        }

        public class AddAttachmentRequest
        {
            public int QuestionIndex { get; set; }
            public string ImageUrl { get; set; } = string.Empty;
        }

        public class RemoveAttachmentRequest
        {
            public int QuestionIndex { get; set; }
            public int AttachmentIndex { get; set; }
        }

        #endregion
    }
}