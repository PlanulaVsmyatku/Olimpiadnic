using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Olimpiadnic.Models;
using Olimpiadnic.Models.HomeModels;
using Olimpiadnic.Models.OlympiadModels;
using System.Diagnostics;
using System.Security.Claims;

namespace Olimpiadnic.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IAntiforgery _antiforgery;

        public HomeController(ILogger<HomeController> logger, IAntiforgery antiforgery)
        {
            _logger = logger;
            _antiforgery = antiforgery;
        }

        // GET: /Home/Index
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index(OlympiadSearchViewModel? searchModel)
        {
            var viewModel = new IndexViewModel
            {
                SearchModel = searchModel ?? new OlympiadSearchViewModel(),
                Olympiads = new List<OlympiadCardViewModel>(),
                HasResults = false,
                TotalCount = 0
            };

            // TODO: Заменить на реальные данные из БД
            var allOlympiads = GetMockOlympiads();

            // Фильтрация
            var filteredOlympiads = allOlympiads.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchModel?.SearchTitle))
            {
                filteredOlympiads = filteredOlympiads.Where(o =>
                    o.Title.Contains(searchModel.SearchTitle, StringComparison.OrdinalIgnoreCase));
            }

            if (searchModel?.StartDateFrom != null)
            {
                filteredOlympiads = filteredOlympiads.Where(o => o.EventStart >= searchModel.StartDateFrom);
            }

            if (searchModel?.StartDateTo != null)
            {
                filteredOlympiads = filteredOlympiads.Where(o => o.EventStart <= searchModel.StartDateTo);
            }

            if (searchModel?.EndDateFrom != null)
            {
                filteredOlympiads = filteredOlympiads.Where(o => o.EventEnd >= searchModel.EndDateFrom);
            }

            if (searchModel?.EndDateTo != null)
            {
                filteredOlympiads = filteredOlympiads.Where(o => o.EventEnd <= searchModel.EndDateTo);
            }

            viewModel.Olympiads = filteredOlympiads.ToList();
            viewModel.HasResults = viewModel.Olympiads.Any();
            viewModel.TotalCount = viewModel.Olympiads.Count;

            // Проверяем, записан ли пользователь на олимпиады
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    // TODO: Получить ID олимпиад, на которые записан пользователь из БД
                    // var registeredOlympiadIds = await _context.OlimpiadParticipants
                    //     .Where(p => p.UserId == userId)
                    //     .Select(p => p.OlympiadId)
                    //     .ToListAsync();

                    var registeredOlympiadIds = new List<int>(); // Заглушка

                    foreach (var olympiad in viewModel.Olympiads)
                    {
                        olympiad.IsUserRegistered = registeredOlympiadIds.Contains(olympiad.OlympiadId);
                    }
                }
            }

            // Сохраняем AntiForgeryToken для использования в AJAX
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

        private List<OlympiadCardViewModel> GetMockOlympiads()
        {
            return new List<OlympiadCardViewModel>
            {
                new OlympiadCardViewModel
                {
                    OlympiadId = 1,
                    Title = "Олимпиада по математике",
                    Description = "Международная олимпиада по математике для школьников 9-11 классов. Участников ждут интересные задачи и ценные призы.",
                    ImageUrl = "/images/math-olympiad.jpg",
                    EventStart = new DateTime(2026, 5, 15, 10, 0, 0),
                    EventEnd = new DateTime(2026, 5, 20, 18, 0, 0),
                    RegistOpen = new DateTime(2026, 5, 1, 0, 0, 0),
                    RegistClosed = new DateTime(2026, 5, 13, 23, 59, 0),
                    Status = "Регистрация открыта",
                    IsUserRegistered = false
                },
                new OlympiadCardViewModel
                {
                    OlympiadId = 2,
                    Title = "Олимпиада по программированию",
                    Description = "Всероссийская олимпиада по программированию и алгоритмам. Решайте реальные задачи от ведущих IT-компаний.",
                    ImageUrl = "/images/programming-olympiad.jpg",
                    EventStart = new DateTime(2026, 5, 11, 9, 0, 0),
                    EventEnd = new DateTime(2026, 5, 15, 20, 0, 0),
                    RegistOpen = new DateTime(2026, 4, 1, 0, 0, 0),
                    RegistClosed = new DateTime(2026, 5, 5, 23, 59, 0),
                    Status = "Завершена",
                    IsUserRegistered = false
                },
                new OlympiadCardViewModel
                {
                    OlympiadId = 3,
                    Title = "Олимпиада по физике",
                    Description = "Региональная олимпиада по физике для 10-11 классов. Проверьте свои знания в области точных наук.",
                    ImageUrl = "/images/physics-olympiad.jpg",
                    EventStart = new DateTime(2026, 3, 20, 10, 0, 0),
                    EventEnd = new DateTime(2026, 3, 25, 18, 0, 0),
                    RegistOpen = new DateTime(2026, 2, 15, 0, 0, 0),
                    RegistClosed = new DateTime(2026, 3, 15, 23, 59, 0),
                    Status = "Завершена",
                    IsUserRegistered = true
                },
                new OlympiadCardViewModel
                {
                    OlympiadId = 4,
                    Title = "Олимпиада по информатике",
                    Description = "Открытая олимпиада по информатике и ИТ. Идеально подходит для начинающих и опытных программистов.",
                    ImageUrl = "/images/informatics-olympiad.jpg",
                    EventStart = new DateTime(2026, 6, 5, 11, 0, 0),
                    EventEnd = new DateTime(2026, 6, 10, 19, 0, 0),
                    RegistOpen = new DateTime(2026, 5, 22, 0, 0, 0),
                    RegistClosed = new DateTime(2026, 6, 1, 23, 59, 0),
                    Status = "Регистрация скоро",
                    IsUserRegistered = false
                }
            };
        }

        #endregion
    }

    // DTO для запроса регистрации
    public class RegisterOlympiadRequest
    {
        public int OlympiadId { get; set; }
    }
}

