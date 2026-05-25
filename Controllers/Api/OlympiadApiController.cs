using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Olimpiadnic.Models.OlympiadModels;
using Olimpiadnic.Services;
using Olimpiadnic.Services.Repos;
using System.Security.Claims;
namespace Olimpiadnic.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/olympiad")]
    public class OlympiadApiController : ControllerBase
    {
        private readonly IOlympiadRepository _olympiadRepository;
        private readonly IOlympiadSessionService _sessionService;
        private readonly ILogger<OlympiadApiController> _logger;

        public OlympiadApiController(
            IOlympiadRepository olympiadRepository,
            IOlympiadSessionService sessionService,
            ILogger<OlympiadApiController> logger)
        {
            _olympiadRepository = olympiadRepository;
            _sessionService = sessionService;
            _logger = logger;
        }

        #region Вспомогательные методы

        /// <summary>
        /// Получение текущей сессии с проверкой прав
        /// </summary>
        private async Task<(bool success, OlympiadParticipationViewModel? session, string? error)> GetValidSessionAsync(int olympiadId, int participantId)
        {
            // Проверяем права пользователя
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return (false, null, "Пользователь не авторизован");
            }

            // Проверяем, что participantId принадлежит пользователю
            var participant = await _olympiadRepository.GetOrCreateParticipantAsync(olympiadId, userId);
            if (participant.ParticipantId != participantId)
            {
                return (false, null, "Доступ запрещен");
            }

            var session = _sessionService.GetSession(olympiadId, participantId);
            if (session == null)
            {
                return (false, null, "Сессия не найдена. Пожалуйста, начните прохождение заново.");
            }

            return (true, session, null);
        }

        /// <summary>
        /// Сохранение ответа на текущий вопрос в сессию
        /// </summary>
        private void SaveAnswerToSession(OlympiadParticipationViewModel session, SaveAnswerRequest request)
        {
            //существует ли сессия и находится ли индекс вопроса в границах
            if (session == null || request.CurrentQuestionIndex < 0 || request.CurrentQuestionIndex >= session.Questions.Count)
                return;

            var currentQuestion = session.Questions[request.CurrentQuestionIndex];

            if (currentQuestion.Type.StartsWith("auto"))
            {
                currentQuestion.SelectedOptionIds = request.SelectedOptionIds ?? new List<int>();
                foreach (var option in currentQuestion.Options)
                {
                    option.IsSelected = currentQuestion.SelectedOptionIds.Contains(option.OptionId);
                }
            }
            else if (currentQuestion.Type == "manual")
            {
                currentQuestion.ManualAnswer = request.ManualAnswer?.Trim() ?? string.Empty;
            }

            _sessionService.UpdateAnswer(session, request.CurrentQuestionIndex, currentQuestion);
        }

        #endregion

        #region API Методы

        /// <summary>
        /// Получение данных для отображения пагинации
        /// </summary>
        [HttpGet("navigation/{olympiadId}/{participantId}")]
        public async Task<IActionResult> GetNavigationData(int olympiadId, int participantId)
        {
            var validation = await GetValidSessionAsync(olympiadId, participantId);
            if (!validation.success)
            {
                return BadRequest(new { success = false, message = validation.error });
            }

            var session = validation.session!;

            // Формируем данные о вопросах для пагинации
            var questionsStatus = session.Questions.Select((q, idx) => new
            {
                index = idx,
                number = idx + 1,
                isAnswered = IsQuestionAnswered(q),
                isCurrent = idx == session.CurrentQuestionIndex
            }).ToList();

            return Ok(new
            {
                success = true,
                currentIndex = session.CurrentQuestionIndex,
                totalQuestions = session.TotalQuestions,
                questions = questionsStatus
            });
        }

        /// <summary>
        /// Получение конкретного вопроса по индексу
        /// </summary>
        [HttpPost("question")]
        public async Task<IActionResult> GetQuestion([FromBody] GetQuestionRequest request)
        {
            var validation = await GetValidSessionAsync(request.OlympiadId, request.ParticipantId);
            if (!validation.success)
            {
                return BadRequest(new { success = false, message = validation.error });
            }

            var session = validation.session!;

            // Проверяем корректность индекса
            if (request.QuestionIndex < 0 || request.QuestionIndex >= session.TotalQuestions)
            {
                return BadRequest(new { success = false, message = "Некорректный номер вопроса" });
            }

            // Сохраняем ответ на текущий вопрос перед переходом (если есть изменения)
            if (request.CurrentQuestionIndex >= 0 && request.CurrentQuestionIndex != request.QuestionIndex)
            {
                SaveAnswerToSession(session, new SaveAnswerRequest
                {
                    CurrentQuestionIndex = request.CurrentQuestionIndex,
                    SelectedOptionIds = request.CurrentSelectedOptionIds,
                    ManualAnswer = request.CurrentManualAnswer
                });
            }

            // Обновляем текущий индекс в сессии
            _sessionService.UpdateCurrentQuestionIndex(session, request.QuestionIndex);

            // Получаем свежую сессию после обновления
            var updatedSession = _sessionService.GetSession(request.OlympiadId, request.ParticipantId);
            var question = updatedSession!.CurrentQuestion;

            // Формируем ответ для клиента
            var result = new
            {
                success = true,
                question = new
                {
                    id = question.QuestionId,
                    number = question.OrderNumber,
                    text = question.Description,
                    type = question.Type,
                    attachments = question.Attachments,
                    options = question.Options.Select(o => new
                    {
                        id = o.OptionId,
                        text = o.OptionText,
                        isSelected = o.IsSelected
                    }),
                    selectedOptionIds = question.SelectedOptionIds,
                    manualAnswer = question.ManualAnswer
                },
                currentIndex = request.QuestionIndex,
                totalQuestions = session.TotalQuestions,
                hasPrevious = request.QuestionIndex > 0,
                hasNext = request.QuestionIndex + 1 < session.TotalQuestions
            };

            return Ok(result);
        }

        /// <summary>
        /// Сохранение ответа и навигация (Назад/Далее)
        /// </summary>
        [HttpPost("navigate")]
        public async Task<IActionResult> Navigate([FromBody] NavigateRequest request)
        {
            var validation = await GetValidSessionAsync(request.OlympiadId, request.ParticipantId);
            if (!validation.success)
            {
                return BadRequest(new { success = false, message = validation.error });
            }

            var session = validation.session!;

            // Сохраняем ответ на текущий вопрос
            SaveAnswerToSession(session, new SaveAnswerRequest
            {
                CurrentQuestionIndex = request.CurrentQuestionIndex,
                SelectedOptionIds = request.SelectedOptionIds,
                ManualAnswer = request.ManualAnswer
            });

            // Определяем новый индекс
            int newIndex = request.CurrentQuestionIndex;
            switch (request.Direction)
            {
                case "previous":
                    newIndex = request.CurrentQuestionIndex - 1;
                    break;
                case "next":
                    newIndex = request.CurrentQuestionIndex + 1;
                    break;
                case "goto":
                    newIndex = request.TargetIndex;
                    break;
            }

            // Проверяем границы
            if (newIndex < 0 || newIndex >= session.TotalQuestions)
            {
                return BadRequest(new { success = false, message = "Некорректный переход" });
            }

            // Обновляем индекс в сессии
            _sessionService.UpdateCurrentQuestionIndex(session, newIndex);

            // Получаем обновленный вопрос
            var updatedSession = _sessionService.GetSession(request.OlympiadId, request.ParticipantId);
            var question = updatedSession!.CurrentQuestion;

            return Ok(new
            {
                success = true,
                newIndex = newIndex,
                question = new
                {
                    id = question.QuestionId,
                    number = question.OrderNumber,
                    text = question.Description,
                    type = question.Type,
                    attachments = question.Attachments,
                    options = question.Options.Select(o => new
                    {
                        id = o.OptionId,
                        text = o.OptionText,
                        isSelected = o.IsSelected
                    }),
                    selectedOptionIds = question.SelectedOptionIds,
                    manualAnswer = question.ManualAnswer
                },
                hasPrevious = newIndex > 0,
                hasNext = newIndex + 1 < session.TotalQuestions
            });
        }

        /// <summary>
        /// Сохранение ответа без навигации (автосохранение)
        /// </summary>
        [HttpPost("save-answer")]
        public async Task<IActionResult> SaveAnswerOnly([FromBody] SaveAnswerOnlyRequest request)
        {
            var validation = await GetValidSessionAsync(request.OlympiadId, request.ParticipantId);
            if (!validation.success)
            {
                return BadRequest(new { success = false, message = validation.error });
            }

            var session = validation.session!;

            SaveAnswerToSession(session, new SaveAnswerRequest
            {
                CurrentQuestionIndex = request.QuestionIndex,
                SelectedOptionIds = request.SelectedOptionIds,
                ManualAnswer = request.ManualAnswer
            });

            // Получаем обновленный статус вопроса
            var isAnswered = IsQuestionAnswered(session.Questions[request.QuestionIndex]);

            return Ok(new
            {
                success = true,
                message = "Ответ сохранен",
                isAnswered = isAnswered,
                answeredCount = session.Questions.Count(q => IsQuestionAnswered(q)),
                totalQuestions = session.TotalQuestions
            });
        }

        /// <summary>
        /// Проверка возможности завершения олимпиады
        /// </summary>
        [HttpPost("check-submit")]
        public async Task<IActionResult> CheckSubmit([FromBody] CheckSubmitRequest request)
        {
            var validation = await GetValidSessionAsync(request.OlympiadId, request.ParticipantId);
            if (!validation.success)
            {
                return BadRequest(new { success = false, message = validation.error });
            }

            var session = validation.session!;

            // Сохраняем текущий ответ
            SaveAnswerToSession(session, new SaveAnswerRequest
            {
                CurrentQuestionIndex = request.CurrentQuestionIndex,
                SelectedOptionIds = request.SelectedOptionIds,
                ManualAnswer = request.ManualAnswer
            });

            // Получаем свежую сессию
            var updatedSession = _sessionService.GetSession(request.OlympiadId, request.ParticipantId);

            var unansweredQuestions = updatedSession!.Questions
                .Select((q, idx) => new { q, idx })
                .Where(x => !IsQuestionAnswered(x.q))
                .Select(x => x.idx + 1)
                .ToList();

            if (unansweredQuestions.Any())
            {
                return Ok(new
                {
                    success = true,
                    canSubmit = false,
                    unansweredCount = unansweredQuestions.Count,
                    unansweredNumbers = unansweredQuestions,
                    message = $"Вы не ответили на вопросы: {string.Join(", ", unansweredQuestions)}"
                });
            }

            return Ok(new
            {
                success = true,
                canSubmit = true,
                message = "Все вопросы отвечены. Вы можете завершить олимпиаду."
            });
        }

        #region Завершение олимпиады

        /// <summary>
        /// Финальное завершение олимпиады с пересчётом баллов
        /// </summary>
        [HttpPost("finalize")]
        public async Task<IActionResult> FinalizeOlympiad([FromBody] FinalizeRequest request)
        {
            var validation = await GetValidSessionAsync(request.OlympiadId, request.ParticipantId);
            if (!validation.success)
            {
                return BadRequest(new { success = false, message = validation.error });
            }

            var session = validation.session!;

            // Сохраняем последний ответ, если есть
            if (request.CurrentQuestionIndex >= 0 && request.CurrentQuestionIndex < session.Questions.Count)
            {
                SaveAnswerToSession(session, new SaveAnswerRequest
                {
                    CurrentQuestionIndex = request.CurrentQuestionIndex,
                    SelectedOptionIds = request.SelectedOptionIds,
                    ManualAnswer = request.ManualAnswer
                });

                // Также сохраняем в БД все ответы из сессии
                await SaveAllSessionAnswersToDatabase(session);
            }

            // Завершаем олимпиаду в БД
            var finalizeResult = await _olympiadRepository.FinalizeOlympiadAsync(request.ParticipantId);

            if (!finalizeResult.Success)
            {
                return BadRequest(new { success = false, message = finalizeResult.ErrorMessage });
            }

            // Удаляем сессию
            _sessionService.DeleteSession(session);

            _logger.LogInformation($"Олимпиада {request.OlympiadId} завершена для участника {request.ParticipantId}. Результат: {finalizeResult.TotalScore}");

            return Ok(new
            {
                success = true,
                totalScore = finalizeResult.TotalScore,
                autoScore = finalizeResult.AutoScore,
                manualPendingCount = finalizeResult.ManualPendingCount,
                message = finalizeResult.ManualPendingCount > 0
                    ? $"Олимпиада завершена! Автоматический балл: {finalizeResult.AutoScore}. Остальные ответы будут проверены позже."
                    : $"Олимпиада завершена! Ваш результат: {finalizeResult.TotalScore} баллов."
            });
        }

        /// <summary>
        /// Сохранение всех ответов из сессии в БД (перед завершением)
        /// </summary>
        private async Task SaveAllSessionAnswersToDatabase(OlympiadParticipationViewModel session)
        {
            foreach (var question in session.Questions)
            {
                // Получаем снапшот вопроса
                var snapshot = await _olympiadRepository.GetQuestionSnapshotByOriginalIdAsync(
                    session.OlympiadId, question.QuestionId);

                if (snapshot == null) continue;

                object answerData;
                if (question.Type.StartsWith("auto"))
                {
                    answerData = question.SelectedOptionIds ?? new List<int>();
                }
                else
                {
                    answerData = question.ManualAnswer ?? string.Empty;
                }

                // Сохраняем в БД
                await _olympiadRepository.SaveAnswerAndCheckAsync(
                    session.ParticipantId,
                    snapshot.QuestSnapshotId,
                    answerData);
            }
        }

        #endregion

        // Добавляем модель запроса
        public class FinalizeRequest
        {
            public int OlympiadId { get; set; }
            public int ParticipantId { get; set; }
            public int CurrentQuestionIndex { get; set; }
            public List<int>? SelectedOptionIds { get; set; }
            public string? ManualAnswer { get; set; }
        }



        #endregion

        #region Вспомогательные методы

        private bool IsQuestionAnswered(QuestionParticipationViewModel question)
        {
            if (question.Type.StartsWith("auto"))
            {
                return question.SelectedOptionIds != null && question.SelectedOptionIds.Any();
            }
            else if (question.Type == "manual")
            {
                return !string.IsNullOrWhiteSpace(question.ManualAnswer);
            }
            return false;
        }

        #endregion
    }

    #region Request модели

    public class GetQuestionRequest
    {
        public int OlympiadId { get; set; }
        public int ParticipantId { get; set; }
        public int QuestionIndex { get; set; }
        public int CurrentQuestionIndex { get; set; }
        public List<int>? CurrentSelectedOptionIds { get; set; }
        public string? CurrentManualAnswer { get; set; }
    }

    public class NavigateRequest
    {
        public int OlympiadId { get; set; }
        public int ParticipantId { get; set; }
        public int CurrentQuestionIndex { get; set; }
        public string Direction { get; set; } = string.Empty; // previous, next, goto
        public int TargetIndex { get; set; } // для goto
        public List<int>? SelectedOptionIds { get; set; }
        public string? ManualAnswer { get; set; }
    }

    public class SaveAnswerRequest
    {
        public int OlympiadId { get; set; }
        public int ParticipantId { get; set; }
        public int CurrentQuestionIndex { get; set; }
        public List<int>? SelectedOptionIds { get; set; }
        public string? ManualAnswer { get; set; }
    }

    public class SaveAnswerOnlyRequest
    {
        public int OlympiadId { get; set; }
        public int ParticipantId { get; set; }
        public int QuestionIndex { get; set; }
        public List<int>? SelectedOptionIds { get; set; }
        public string? ManualAnswer { get; set; }
    }

    public class CheckSubmitRequest
    {
        public int OlympiadId { get; set; }
        public int ParticipantId { get; set; }
        public int CurrentQuestionIndex { get; set; }
        public List<int>? SelectedOptionIds { get; set; }
        public string? ManualAnswer { get; set; }
    }

    #endregion
}

