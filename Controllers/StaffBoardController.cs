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
                // Проверяем права
                var isAuthor = await _context.OlympStaffs
                    .AnyAsync(s => s.OlympId == id.Value && s.UserId == userId && s.OlimpRole == "author");
                var isAdmin = User.IsInRole("admin");

                if (!isAuthor && !isAdmin)
                    return Forbid();

                // Режим редактирования - загружаем из БД
                var olympiad = await _olympiadRepository.GetOlympiadForEditAsync(id.Value);

                if (olympiad == null)
                    return NotFound();

                olympiad.IsEditMode = true;

                // Проверяем наличие черновика в сессии
                bool draftExists = _sessionService.SessionExists(userId);

                if (draftExists)
                {
                    var draft = await _sessionService.GetSessionAsync(userId);

                    if (draft != null && draft.Questions.Any() && !string.IsNullOrEmpty(draft.Title))
                    {
                        // Сохраняем информацию о черновике в TempData
                        TempData["HasDraft"] = true;
                        TempData["DraftTitle"] = draft.Title;
                        TempData["DraftQuestionsCount"] = draft.Questions.Count;
                        TempData["OlympiadIdForEdit"] = id.Value;

                        // Проверяем, хочет ли пользователь загрузить черновик
                        var loadDraft = Request.Query.ContainsKey("loadDraft") && Request.Query["loadDraft"] == "true";
                        var discardDraft = Request.Query.ContainsKey("discardDraft") && Request.Query["discardDraft"] == "true";

                        if (loadDraft)
                        {
                            // Пользователь выбрал загрузить черновик
                            draft.IsEditMode = true;
                            draft.OlympiadId = olympiad.OlympiadId;
                            // Сохраняем черновик в сессию (он уже там есть)
                            await _sessionService.SaveSessionAsync(userId, draft);
                            TempData["Success"] = $"Черновик олимпиады \"{draft.Title}\" загружен. Можете продолжить редактирование.";
                            return View(draft);
                        }
                        else if (discardDraft)
                        {
                            // Пользователь выбрал отменить черновик и начать с БД
                            await _sessionService.DeleteSessionAsync(userId);
                            // Сохраняем данные из БД в новую сессию
                            await _sessionService.SaveSessionAsync(userId, olympiad);
                            TempData["Success"] = "Черновик отменён. Загружена последняя версия из базы данных.";
                            return View(olympiad);
                        }

                        // Если параметры не указаны - показываем страницу с уведомлением о черновике
                        // и передаём оба варианта во ViewBag
                        ViewBag.HasDraft = true;
                        ViewBag.DraftInfo = new { draft.Title, QuestionsCount = draft.Questions.Count };
                        ViewBag.OlympiadFromDb = olympiad;

                        // Возвращаем представление с черновиком по умолчанию, но с уведомлением
                        TempData["Warning"] = $"У вас есть незавершённый черновик олимпиады \"{draft.Title}\" с {draft.Questions.Count} вопросами. " +
                            $"<a href='?loadDraft=true' class='alert-link'>Загрузить черновик</a> | " +
                            $"<a href='?discardDraft=true' class='alert-link'>Начать с версии из БД</a>";

                        draft.IsEditMode = true;
                        draft.OlympiadId = olympiad.OlympiadId;
                        return View(draft);
                    }
                    else
                    {
                        // Сессия есть, но она пустая или битая - создаём новую из БД
                        await _sessionService.DeleteSessionAsync(userId);
                        await _sessionService.SaveSessionAsync(userId, olympiad);
                        return View(olympiad);
                    }
                }
                else
                {
                    // Черновика нет - создаём новую сессию с данными из БД
                    await _sessionService.SaveSessionAsync(userId, olympiad);
                    return View(olympiad);
                }
            }
            else
            {
                // Режим создания - проверяем черновик в сессии
                var draft = await _sessionService.GetSessionAsync(userId);

                // Проверяем, хочет ли пользователь отменить черновик
                var discardDraft = Request.Query.ContainsKey("discardDraft") && Request.Query["discardDraft"] == "true";

                if (discardDraft)
                {
                    // Пользователь хочет начать новый черновик
                    await _sessionService.DeleteSessionAsync(userId);
                    draft = null;
                    TempData["Success"] = "Черновик отменён. Начат новый черновик.";
                }

                if (draft != null && !string.IsNullOrEmpty(draft.Title) && draft.Questions.Any())
                {
                    // Есть черновик - проверяем, хочет ли пользователь его загрузить
                    var loadDraft = Request.Query.ContainsKey("loadDraft") && Request.Query["loadDraft"] == "true";

                    if (loadDraft)
                    {
                        // Загружаем существующий черновик
                        TempData["Success"] = $"Черновик олимпиады \"{draft.Title}\" загружен. Можете продолжить создание.";
                        return View(draft);
                    }
                    else if (!discardDraft)
                    {
                        // Показываем уведомление о черновике с вариантами выбора
                        TempData["Warning"] = $"У вас есть незавершённый черновик олимпиады \"{draft.Title}\" с {draft.Questions.Count} вопросами. " +
                            $"<a href='?loadDraft=true' class='alert-link'>Продолжить работу с черновиком</a> | " +
                            $"<a href='?discardDraft=true' class='alert-link'>Начать новый черновик</a>";
                        return View(draft);
                    }
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

                // Сохраняем новый черновик в сессию
                await _sessionService.SaveSessionAsync(userId, newModel);

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