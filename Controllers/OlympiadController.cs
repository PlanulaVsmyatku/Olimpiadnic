using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly ILogger<OlympiadController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public OlympiadController(ILogger<OlympiadController> logger, IWebHostEnvironment webHostEnvironment, IOlympiadRepository olympiadRepository)
        {
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _olympiadRepository = olympiadRepository;
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

            // Получаем вопросы
            var questions = await _olympiadRepository.GetQuestionsForParticipationAsync(id);
            if (!questions.Any())
            {
                TempData["Error"] = "В этой олимпиаде пока нет вопросов";
                return RedirectToAction("Details", new { id });
            }

            // Получаем или создаём сессию
            var sessionService = HttpContext.RequestServices.GetRequiredService<IOlympiadSessionService>();
            var participation = await sessionService.GetOrCreateSessionAsync(id, userId, participant.ParticipantId, questions);

            // Если есть сохранённые ответы из сессии, загружаем их
            if (participation.Questions.Any() && participation.Questions[0].Options.Count == 0)
            {
                await LoadSavedAnswersToSessionAsync(participation, participant.ParticipantId);
            }

            var viewModel = new OlympiadParticipationViewModel
            {
                OlympiadId = participation.OlympiadId,
                OlympiadTitle = participation.OlympiadTitle,
                ParticipantId = participation.ParticipantId,
                CurrentQuestionIndex = participation.CurrentQuestionIndex,
                TotalQuestions = participation.TotalQuestions,
                IsCompleted = participation.IsCompleted,
                Questions = participation.Questions
            };

            return View(viewModel);
        }

        // Вспомогательный метод для загрузки сохранённых ответов из черновика (если есть)
        private async Task LoadSavedAnswersToSessionAsync(OlympiadParticipationViewModel participation, int participantId)
        {
            // TODO: Загрузка ответов из БД (черновики) - реализуем позже
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAnswer(OlympiadParticipationViewModel model, string action)
        {
            var sessionService = HttpContext.RequestServices.GetRequiredService<IOlympiadSessionService>();
            var session = sessionService.GetSession(model.OlympiadId);

            if (session == null)
            {
                return RedirectToAction("Participate", new { id = model.OlympiadId });
            }

            // Сохраняем ответ на текущий вопрос
            var currentQuestion = session.Questions[model.CurrentQuestionIndex];

            if (currentQuestion.Type == "auto-radio" || currentQuestion.Type == "auto-checkbox")
            {
                currentQuestion.SelectedOptionIds = model.CurrentQuestion.SelectedOptionIds ?? new List<int>();
            }
            else if (currentQuestion.Type == "manual")
            {
                currentQuestion.ManualAnswer = model.CurrentQuestion.ManualAnswer ?? string.Empty;
            }

            sessionService.UpdateAnswer(model.OlympiadId, model.CurrentQuestionIndex, currentQuestion);

            // Навигация в зависимости от действия
            switch (action)
            {
                case "previous":
                    if (model.CurrentQuestionIndex > 0)
                    {
                        session.CurrentQuestionIndex--;
                        sessionService.UpdateAnswer(model.OlympiadId, -1, null);
                    }
                    break;

                case "next":
                    if (model.CurrentQuestionIndex + 1 < session.TotalQuestions)
                    {
                        session.CurrentQuestionIndex++;
                        sessionService.UpdateAnswer(model.OlympiadId, -1, null);
                    }
                    else
                    {
                        // Завершение - переходим к подтверждению
                        return RedirectToAction("ConfirmSubmit", new { id = model.OlympiadId });
                    }
                    break;

                case "submit":
                    return RedirectToAction("ConfirmSubmit", new { id = model.OlympiadId });
            }

            return RedirectToAction("Participate", new { id = model.OlympiadId });
        }

        [Authorize]
        public async Task<IActionResult> ConfirmSubmit(int id)
        {
            var sessionService = HttpContext.RequestServices.GetRequiredService<IOlympiadSessionService>();
            var session = sessionService.GetSession(id);

            if (session == null)
            {
                return RedirectToAction("Participate", new { id });
            }

            // Подсчитываем количество отвеченных вопросов
            var answeredCount = session.Questions.Count(q =>
                (q.Type == "auto-radio" && q.SelectedOptionIds.Any()) ||
                (q.Type == "auto-checkbox" && q.SelectedOptionIds.Any()) ||
                (q.Type == "manual" && !string.IsNullOrWhiteSpace(q.ManualAnswer)));

            var unansweredCount = session.TotalQuestions - answeredCount;

            ViewBag.OlympiadId = id;
            ViewBag.OlympiadTitle = session.OlympiadTitle;
            ViewBag.AnsweredCount = answeredCount;
            ViewBag.UnansweredCount = unansweredCount;
            ViewBag.TotalQuestions = session.TotalQuestions;

            return View();
        }

        #endregion



    }
}