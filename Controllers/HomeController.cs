using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Olimpiadnic.Models;
using Olimpiadnic.Models.HomeModels;
using Olimpiadnic.Models.OlympiadModels;
using Olimpiadnic.Services.Repos;
using System.Diagnostics;
using System.Security.Claims;

namespace Olimpiadnic.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IAntiforgery _antiforgery;
        private readonly IOlympiadRepository _olympRepository;

        public HomeController(ILogger<HomeController> logger, IAntiforgery antiforgery, IOlympiadRepository olympRepository)
        {
            _logger = logger;
            _antiforgery = antiforgery;
            _olympRepository = olympRepository;
        }

        // GET: /Home/Index
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index(OlympiadSearchViewModel? searchModel, int page = 1)
        {
            // Определяем роль пользователя
            var isStaff = User.IsInRole("Staff") || User.IsInRole("Admin");

            if (isStaff)
            {
                // Для сотрудников - дашборд (будет позже)
                return RedirectToAction("Profile", "Profile");
            }

            // Получаем пагинированный список с фильтрацией
            var pagedResult = await _olympRepository.GetActiveOlympiadsPagedFilteredAsync(searchModel, page, 12);

            var viewModel = new IndexViewModel
            {
                SearchModel = pagedResult.SearchModel ?? new OlympiadSearchViewModel(),
                Olympiads = pagedResult.Items.ToList(),
                HasResults = pagedResult.Items.Any(),
                TotalCount = pagedResult.TotalCount,
                CurrentPage = pagedResult.CurrentPage,
                TotalPages = pagedResult.TotalPages,
                PageSize = pagedResult.PageSize
            };

            // Проверяем, записан ли пользователь на олимпиады
            if (User.Identity.IsAuthenticated && viewModel.Olympiads.Any())
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    var olympiadIds = viewModel.Olympiads.Select(o => o.OlympiadId).ToList();
                    var registeredParticipations = await _olympRepository
                        .GetParticipantsByUserAndOlympiadIdsAsync(userId, olympiadIds);

                    foreach (var olympiad in viewModel.Olympiads)
                    {
                        olympiad.IsUserRegistered = registeredParticipations
                            .Any(p => p.OlympId == olympiad.OlympiadId);
                    }
                }
            }

            // Сохраняем AntiForgeryToken
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            HttpContext.Items["AntiforgeryToken"] = tokens.RequestToken;

            return View(viewModel);
        }

        // POST: /Home/RegisterForOlympiad
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterForOlympiad([FromBody] RegisterOlympiadRequest request)
        {
            try
            {
                if (request == null || request.OlympiadId <= 0)
                {
                    return Json(new { success = false, message = "Некорректный идентификатор олимпиады" });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, requiresLogin = true, message = "Требуется авторизация" });
                }

                // TODO: Проверка, существует ли олимпиада и открыта ли регистрация
                // var olympiad = await _context.Olimpiads.FindAsync(request.OlympiadId);
                // if (olympiad == null)
                // {
                //     return Json(new { success = false, message = "Олимпиада не найдена" });
                // }
                //
                // var now = DateTime.Now;
                // if (now < olympiad.RegistOpen || now > olympiad.RegistClosed)
                // {
                //     return Json(new { success = false, message = "Регистрация на эту олимпиаду закрыта" });
                // }

                // TODO: Проверка, не записан ли уже пользователь
                // var existingParticipant = await _context.OlimpiadParticipants
                //     .FirstOrDefaultAsync(p => p.UserId == userId && p.OlympiadId == request.OlympiadId);
                //
                // if (existingParticipant != null)
                // {
                //     return Json(new { success = false, message = "Вы уже записаны на эту олимпиаду" });
                // }

                // TODO: Добавление записи в таблицу участников
                // var participant = new OlimpiadParticipant
                // {
                //     UserId = userId,
                //     OlympiadId = request.OlympiadId,
                //     RegDate = DateTime.Now,
                //     Status = "registered"
                // };
                //
                // _context.OlimpiadParticipants.Add(participant);
                // await _context.SaveChangesAsync();

                // Временная заглушка для тестирования
                await Task.Delay(100);

                return Json(new { success = true, message = "Вы успешно записаны на олимпиаду!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при записи на олимпиаду. OlympiadId: {OlympiadId}, UserId: {UserId}",
                    request?.OlympiadId, User.FindFirstValue(ClaimTypes.NameIdentifier));
                return Json(new { success = false, message = "Произошла внутренняя ошибка сервера" });
            }
        }

        // GET: /Home/GetAntiForgeryToken
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetAntiForgeryToken()
        {
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            return Json(new { token = tokens.RequestToken });
        }

        // GET: /Home/MyOlympiads
        [HttpGet]
        [Authorize]
        public IActionResult MyOlympiads()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // TODO: Получить олимпиады пользователя из БД
            return View();
        }

        // GET: /Home/Privacy
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        // GET: /Home/AccessDenied
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        #region Вспомогательные методы


        #endregion
    }

    // DTO для запроса регистрации
    public class RegisterOlympiadRequest
    {
        public int OlympiadId { get; set; }
    }
}

