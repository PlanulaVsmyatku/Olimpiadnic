using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Olimpiadnic.Extensions;
using Olimpiadnic.Models.OlympiadModels;
using Olimpiadnic.Models.RoleModels;
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

        [HttpPost]
        [Authorize]
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

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, requiresLogin = true, message = "Требуется авторизация" });
                }

                var participants = await _olympiadRepository.GetParticipantsByOlympiadIdAsync(id);
                if (participants.Any(p => p.UserId == int.Parse(userId)))
                {
                    return Json(new { success = false, message = "Вы уже записаны на эту олимпиаду" });
                }

                // TODO: Добавить запись в таблицу OlympiadParticipants
                // var participant = new OlympiadParticipant
                // {
                //     UserId = userId,
                //     OlympId = id,
                //     RegDate = now,
                //     Status = "registered"
                // };
                // _context.OlympiadParticipants.Add(participant);
                // await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Вы успешно записаны на олимпиаду!" });
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
            // TODO: Страница прохождения олимпиады
            return Content($"Страница прохождения олимпиады {id} (будет реализована позже)");
        }

        #endregion
        
    }
}