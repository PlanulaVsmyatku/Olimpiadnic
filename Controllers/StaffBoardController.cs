// StaffBoardController.cs - добавляем метод Create
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Olimpiadnic.Data;
using Olimpiadnic.Models.RoleModels;
using Olimpiadnic.Services.Repos;
using Olimpiadnic.Services.Session;
using System.Security.Claims;

namespace Olimpiadnic.Controllers
{
    [Authorize(Roles = "staff,admin")]
    public class StaffBoardController : Controller
    {
        private readonly ILogger<StaffBoardController> _logger;
        private readonly IOlympiadRepository _olympiadRepository;
        private readonly IOlympiadEditorSessionService _sessionService;
        private readonly AppDbContext _context;

        public StaffBoardController(
            ILogger<StaffBoardController> logger,
            IOlympiadRepository olympiadRepository,
            IOlympiadEditorSessionService sessionService,
            AppDbContext context)
        {
            _logger = logger;
            _olympiadRepository = olympiadRepository;
            _sessionService = sessionService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? id)
        {
            var userId = GetCurrentUserId();

            if (id.HasValue)
            {
                // Проверяем, есть ли черновик
                var draft = await _sessionService.GetSessionAsync(userId);
                if (draft != null && draft.Questions.Any() && !string.IsNullOrEmpty(draft.Title))
                {
                    // Сохраняем ID черновика в TempData для возможного восстановления
                    TempData["HasDraft"] = true;
                    TempData["DraftTitle"] = draft.Title;
                    TempData["DraftQuestionsCount"] = draft.Questions.Count;
                    TempData["OlympiadIdForEdit"] = id.Value;
                }

                // Режим редактирования - загружаем из БД
                var olympiad = await _olympiadRepository.GetOlympiadForEditAsync(id.Value);

                if (olympiad == null)
                    return NotFound();

                // Проверяем права
                var isAuthor = await _context.OlympStaffs
                    .AnyAsync(s => s.OlympId == id.Value && s.UserId == userId && s.OlimpRole == "author");
                var isAdmin = User.IsInRole("admin");

                if (!isAuthor && !isAdmin)
                    return Forbid();

                olympiad.IsEditMode = true;

                // Если есть черновик, показываем предупреждение, но НЕ удаляем сессию автоматически
                if (TempData["HasDraft"] as bool? == true)
                {
                    TempData["Warning"] = $"У вас есть незавершённый черновик олимпиады \"{TempData["DraftTitle"]}\" с {TempData["DraftQuestionsCount"]} вопросами. Черновик сохранён в сессии.";
                }
                else
                {
                    // Очищаем сессию только если нет черновика
                    await _sessionService.DeleteSessionAsync(userId);
                }

                return View(olympiad);
            }
            else
            {
                // Режим создания - проверяем черновик в сессии
                var draft = await _sessionService.GetSessionAsync(userId);

                if (draft != null && !string.IsNullOrEmpty(draft.Title))
                {
                    // Есть черновик - загружаем его
                    return View(draft);
                }

                // Новый черновик
                var newModel = new CreateOlympiadViewModel
                {
                    OlympiadId = 0,
                    IsEditMode = false,
                    Title = "",
                    Description = "",
                    Credentials = "",
                    ImageUrl = "",
                    RegistOpen = DateTime.Now.AddDays(7),
                    RegistClosed = DateTime.Now.AddDays(14),
                    EventStart = DateTime.Now.AddDays(21),
                    EventEnd = DateTime.Now.AddDays(28),
                    Questions = new List<QuestionEditorViewModel>()
                };

                return View(newModel);
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }
    }
}