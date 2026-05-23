using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Olimpiadnic.Data;
using Olimpiadnic.Entities;
using Olimpiadnic.Extensions;
using Olimpiadnic.Models.OlympiadModels;
using Olimpiadnic.Models.RoleModels;
using Olimpiadnic.Services;
using Olimpiadnic.Services.Repos;
using System.Security.Claims;

namespace Olimpiadnic.Controllers
{
    [Authorize]
    public class OlympiadController : Controller
    {
        private readonly IOlympiadRepository _olympiadRepository;
        private readonly IOlympiadSessionService _sessionService;
        private readonly ILogger<OlympiadController> _logger;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public OlympiadController(ILogger<OlympiadController> logger, IWebHostEnvironment webHostEnvironment, IOlympiadRepository olympiadRepository,
                AppDbContext context, IOlympiadSessionService sessionService)
        {
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _olympiadRepository = olympiadRepository;
            _context = context;
            _sessionService = sessionService;
        }



        #region Просмотр олимпиады (для всех)

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var olympiad = await _olympiadRepository.GetOlympiadByIdAsync(id);

            if (olympiad == null)
            {
                return NotFound();
            }

            var totalQuestions = await _olympiadRepository.GetTotalQuestionsCountAsync(id);
            var isRegistered = false;

            // Проверяем, авторизован ли пользователь и зарегистрирован ли
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    var participants = await _olympiadRepository.GetParticipantsByOlympiadIdAsync(id);
                    isRegistered = participants.Any(p => p.UserId == int.Parse(userId));
                }
            }

            // Определяем статус
            var now = DateTime.Now;
            string status;

            if (now >= olympiad.EventStart && now <= olympiad.EventEnd)
                status = "in_progress";
            else if (now > olympiad.EventEnd)
                status = "ended";
            else
                status = "available";

            var viewModel = new OlympiadDetailsViewModel
            {
                OlympiadId = olympiad.OlympId,
                Title = olympiad.Title,
                ImageUrl = olympiad.ImageUrl ?? string.Empty,
                Description = olympiad.Description ?? string.Empty,
                Credentials = olympiad.Credentials,
                Status = status,
                EventStart = olympiad.EventStart,
                EventEnd = olympiad.EventEnd,
                RegistOpen = olympiad.RegistOpen,
                RegistClosed = olympiad.RegistClosed,
                TotalQuestions = totalQuestions,
                IsRegistered = isRegistered,
                CanParticipate = isRegistered && now >= olympiad.EventStart && now <= olympiad.EventEnd
            };

            return View(viewModel);
        }

        #endregion

        #region Просмотр результатов олимпиады участника
        [Authorize]
        public async Task<IActionResult> Results(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Получаем участника
            var participant = await _olympiadRepository.GetOrCreateParticipantAsync(id, userId);

            // Проверяем, что пользователь имеет право смотреть результаты
            var isOwner = participant.UserId == userId;
            var isStaff = User.IsInRole("Admin") || User.IsInRole("Staff");

            if (!isOwner && !isStaff)
            {
                TempData["Error"] = "У вас нет доступа к этим результатам";
                return RedirectToAction("Details", new { id });
            }

            // Если олимпиада ещё не завершена, но пользователь её завершил
            if (participant.Status != "completed" && participant.CompletedAt == null)
            {
                TempData["Info"] = "Вы ещё не завершили олимпиаду";
                return RedirectToAction("Participate", new { id });
            }

            var results = await _olympiadRepository.GetParticipantResultsForDisplayAsync(participant.ParticipantId);

            if (results == null)
            {
                TempData["Error"] = "Результаты не найдены";
                return RedirectToAction("Details", new { id });
            }

            return View(results);
        }
        #endregion

        #region Прохождение олимпиады (для участников)
        // регистрация по ajax-запросу без доп страницы
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(int id)
        {
            try
            {
                var olympiad = await _olympiadRepository.GetOlympiadByIdAsync(id);
                if (olympiad == null)
                {
                    return Json(new { success = false, message = "Олимпиада не найдена" });
                }

                var now = DateTime.Now;
                if (now < olympiad.RegistOpen || now > olympiad.RegistClosed)
                {
                    return Json(new { success = false, message = "Регистрация на эту олимпиаду закрыта" });
                }

                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Json(new { success = false, requiresLogin = true, message = "Требуется авторизация" });
                }

                var isRegistered = await _olympiadRepository.IsUserRegisteredAsync(id, userId);
                if (isRegistered)
                {
                    return Json(new { success = false, message = "Вы уже записаны на эту олимпиаду" });
                }

                var result = await _olympiadRepository.RegisterParticipantAsync(id, userId);

                if (result)
                {
                    return Json(new { success = true, message = "Вы успешно записаны на олимпиаду!" });
                }

                return Json(new { success = false, message = "Не удалось записаться. Попробуйте позже." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при записи на олимпиаду {OlympiadId}", id);
                return Json(new { success = false, message = "Произошла ошибка при записи" });
            }
        }

        [Authorize]
        public async Task<IActionResult> Participate(int id)
        {
            #region Проверки
            // Проверка прав и времени
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var olympiad = await _olympiadRepository.GetOlympiadByIdAsync(id);
            if (olympiad == null)
            {
                return NotFound();
            }

            var now = DateTime.Now;

            // Проверка: олимпиада должна идти
            if (now < olympiad.EventStart || now > olympiad.EventEnd)
            {
                TempData["Error"] = "Олимпиада ещё не началась или уже завершилась";
                return RedirectToAction("Details", new { id });
            }

            // Проверка регистрации
            var isRegistered = await _olympiadRepository.IsUserRegisteredAsync(id, userId);
            if (!isRegistered)
            {
                TempData["Error"] = "Вы не зарегистрированы на эту олимпиаду";
                return RedirectToAction("Details", new { id });
            }

            // Проверка, не завершил ли пользователь уже олимпиаду
            var participant = await _olympiadRepository.GetOrCreateParticipantAsync(id, userId);
            if (participant.Status == "completed")
            {
                TempData["Error"] = "Вы уже прошли эту олимпиаду";
                return RedirectToAction("Results", new { id });
            }

            // Обновляем StartedAt если первый заход
            if (participant.StartedAt == null)
            {
                participant.StartedAt = now;
                await _olympiadRepository.UpdateParticipantAsync(participant);
            }
            #endregion

            // Получаем или создаем сессию
            OlympiadParticipationViewModel participation;
            var existingSession = _sessionService.GetSession(id, participant.ParticipantId);

            if (existingSession != null)
            {
                participation = existingSession;
                _logger.LogInformation($"Восстановлена сессия. Текущий вопрос: {participation.CurrentQuestionIndex + 1}, Ответов сохранено: {CountAnsweredQuestions(participation)}");
            }
            else
            {
                // Создаем новую сессию
                var questions = await _olympiadRepository.GetQuestionsForParticipationAsync(id);
                participation = await _sessionService.CreateSessionAsync(id, participant.ParticipantId, questions);
            }

            return View(participation);
        }

        // Вспомогательный метод для подсчета отвеченных вопросов
        private int CountAnsweredQuestions(OlympiadParticipationViewModel participation)
        {
            return participation.Questions.Count(q =>
                (q.Type.StartsWith("auto") && q.SelectedOptionIds != null && q.SelectedOptionIds.Any()) ||
                (q.Type == "manual" && !string.IsNullOrWhiteSpace(q.ManualAnswer))
            );
        }

        // На случай если JS отключён
        [HttpGet]
        public async Task<IActionResult> GetQuestionPartial(int id, int questionIndex)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var participant = await _olympiadRepository.GetOrCreateParticipantAsync(id, int.Parse(userIdClaim));
            var session = _sessionService.GetSession(id, participant.ParticipantId);

            if (session == null)
            {
                return NotFound();
            }

            if (questionIndex >= 0 && questionIndex < session.TotalQuestions)
            {
                _sessionService.UpdateCurrentQuestionIndex(session, questionIndex);
                session = _sessionService.GetSession(id, participant.ParticipantId);
            }

            return PartialView("_QuestionPartial", session!.CurrentQuestion);
        }

        /* API методы уже занимаются сохранением состояния вопроса
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAnswer(SaveAnswerViewModel model, string action)
        {
            try
            {
                // Сохраняем action из параметра или из модели
                string userAction = !string.IsNullOrEmpty(action) ? action : model.Action;

                // Получаем текущую сессию
                var session = _sessionService.GetSession(model.OlympiadId, model.ParticipantId);

                if (session == null)
                {
                    _logger.LogWarning($"Сессия не найдена для олимпиады {model.OlympiadId}");
                    TempData["Error"] = "Сессия не найдена. Пожалуйста, начните прохождение заново.";
                    return RedirectToAction("Participate", new { id = model.OlympiadId });
                }

                // Сохраняем ответ на текущий вопрос, если он изменился
                if (model.CurrentQuestionIndex >= 0 && model.CurrentQuestionIndex < session.Questions.Count)
                {
                    var currentQuestion = session.Questions[model.CurrentQuestionIndex];

                    // Проверяем тип вопроса из сессии, а не из модели
                    if (currentQuestion.Type.StartsWith("auto"))
                    {
                        currentQuestion.SelectedOptionIds = model.SelectedOptionIds ?? new List<int>();
                        foreach (var option in currentQuestion.Options)
                        {
                            option.IsSelected = currentQuestion.SelectedOptionIds.Contains(option.OptionId);
                        }
                    }
                    else if (currentQuestion.Type == "manual")
                    {
                        currentQuestion.ManualAnswer = model.ManualAnswer?.Trim() ?? string.Empty;
                    }

                    _sessionService.UpdateAnswer(session, model.CurrentQuestionIndex, currentQuestion);
                }

                // Навигация в зависимости от действия
                switch (userAction)
                {
                    case "previous":
                        if (model.CurrentQuestionIndex > 0)
                        {
                            _sessionService.UpdateCurrentQuestionIndex(session, model.CurrentQuestionIndex - 1);
                        }
                        break;

                    case "next":
                        if (model.CurrentQuestionIndex + 1 < session.TotalQuestions)
                        {
                            _sessionService.UpdateCurrentQuestionIndex(session, model.CurrentQuestionIndex + 1);
                        }
                        break;

                    case "submit":
                        // Проверяем, все ли вопросы отвечены
                        var unansweredQuestions = session.Questions
                            .Select((q, idx) => new { q, idx })
                            .Where(x => IsQuestionUnanswered(x.q))
                            .ToList();

                        if (unansweredQuestions.Any())
                        {
                            var unansweredNumbers = string.Join(", ", unansweredQuestions.Select(x => x.idx + 1));
                            TempData["Warning"] = $"Вы не ответили на следующие вопросы: {unansweredNumbers}. Вы уверены, что хотите завершить?";
                            return RedirectToAction("ConfirmSubmit", new { id = model.OlympiadId });
                        }

                        return RedirectToAction("ConfirmSubmit", new { id = model.OlympiadId });
                }

                return RedirectToAction("Participate", new { id = model.OlympiadId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сохранении ответа для олимпиады {OlympiadId}", model.OlympiadId);
                TempData["Error"] = "Произошла ошибка при сохранении ответа";
                return RedirectToAction("Participate", new { id = model.OlympiadId });
            }
        }
        */

        // Вспомогательный метод для проверки отвеченности вопроса
        private bool IsQuestionUnanswered(QuestionParticipationViewModel question)
        {
            if (question.Type.StartsWith("auto"))
            {
                return question.SelectedOptionIds == null || !question.SelectedOptionIds.Any();
            }
            else if (question.Type == "manual")
            {
                return string.IsNullOrWhiteSpace(question.ManualAnswer);
            }
            return true;
        }

        // Метод для подтверждения завершения
        [Authorize]
        public async Task<IActionResult> ConfirmSubmit(int id)
        {
            // Нужны проверки - Проверка прав, не закончилась ли олимпиада, зарегестрирован ли пользователь на оилмпиаду
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var participant = await _olympiadRepository.GetOrCreateParticipantAsync(id, int.Parse(userIdClaim));

            var session = _sessionService.GetSession(id, participant.ParticipantId);

            if (session == null)
            {
                return RedirectToAction("Participate", new { id });
            }

            // Подсчитываем статистику для подтверждения
            var answeredCount = session.Questions.Count(q => !IsQuestionUnanswered(q));
            var totalCount = session.TotalQuestions;

            ViewBag.AnsweredCount = answeredCount;
            ViewBag.TotalCount = totalCount;
            ViewBag.UnansweredCount = totalCount - answeredCount;

            return View(session);
        }

        // Метод для финальной отправки
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitOlympiad(int id)
        {
            try
            {
                // Нужны проверки - Проверка прав, не закончилась ли олимпиада, зарегестрирован ли пользователь на оилмпиаду
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var participant = await _olympiadRepository.GetOrCreateParticipantAsync(id, int.Parse(userIdClaim));

                var session = _sessionService.GetSession(id, participant.ParticipantId);

                if (session == null)
                {
                    TempData["Error"] = "Сессия не найдена";
                    return RedirectToAction("Details", new { id });
                }
                int totalScore = 12; //временная заглушка
                // Сохраняем все ответы в БД и вычисляем баллы
                /*
                var totalScore = await _olympiadRepository.SubmitParticipantAnswersAsync(
                    session.ParticipantId,
                    session.Questions);
                */
                // Завершаем олимпиаду
                await _olympiadRepository.CompleteOlympiadAsync(session.ParticipantId, totalScore);

                // Удаляем сессию
                _sessionService.DeleteSession(session);

                _logger.LogInformation($"Олимпиада {id} завершена для участника {session.ParticipantId}. Результат: {totalScore}");

                TempData["Success"] = $"Олимпиада успешно завершена! Ваш результат: {totalScore} баллов.";
                return RedirectToAction("Results", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при завершении олимпиады {OlympiadId}", id);
                TempData["Error"] = "Произошла ошибка при завершении олимпиады";
                return RedirectToAction("Participate", new { id });
            }
        }
        #endregion

    }
}