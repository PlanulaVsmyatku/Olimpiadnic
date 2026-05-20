using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Olimpiadnic.Models;
using Olimpiadnic.Models.OlympiadModels;
using Olimpiadnic.Models.RoleModels;


namespace Olimpiadnic.Controllers
{
    [Authorize(Roles = "Администратор")]
    public class AdminBoardController : Controller
    {
        private readonly ILogger<AdminBoardController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminBoardController(ILogger<AdminBoardController> logger, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: /AdminBoard/Statistics (только для администратора)
        [HttpGet]
        //[Authorize(Roles = "Администратор")]
        public async Task<IActionResult> Statistics()
        {
            var stats = await GetAdminStatisticsAsync();
            return View(stats);
        }

        // GET: /AdminBoard/AllUsers (только для администратора)
        [HttpGet]
        //[Authorize(Roles = "Администратор")]
        public async Task<IActionResult> AllUsers()
        {
            var users = await GetAllUsersAsync();
            return View(users);
        }

        // GET: /AdminBoard/EditUser/{id} (только для администратора)
        [HttpGet]
        //[Authorize(Roles = "Администратор")]
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await GetUserForEditAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: /AdminBoard/EditUser
        [HttpPost]
        //[Authorize(Roles = "Администратор")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserByAdminViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableRoles = UserRoles.AllRoles;
                return View(model);
            }

            // TODO: Сохранить изменения пользователя в БД

            TempData["SuccessMessage"] = "Пользователь успешно обновлён!";
            return RedirectToAction(nameof(AllUsers));
        }

        // GET: /AdminBoard/CreateUser (только для администратора)
        [HttpGet]
        //[Authorize(Roles = "Администратор")]
        public IActionResult CreateUser()
        {
            var model = new EditUserByAdminViewModel
            {
                AvailableRoles = UserRoles.AllRoles,
                IsActive = true
            };
            return View(model);
        }

        // POST: /AdminBoard/CreateUser
        [HttpPost]
        //[Authorize(Roles = "Администратор")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(EditUserByAdminViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableRoles = UserRoles.AllRoles;
                return View(model);
            }

            // TODO: Создать пользователя в БД
            // TODO: Отправить email с временным паролем

            TempData["SuccessMessage"] = "Пользователь успешно создан!";
            return RedirectToAction(nameof(AllUsers));
        }

        // POST: /AdminBoard/DeleteUser (только для администратора)
        [HttpPost]
        //[Authorize(Roles = "Администратор")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            // TODO: Удалить пользователя из БД (или пометить как удалённого)

            TempData["SuccessMessage"] = "Пользователь удалён!";
            return RedirectToAction(nameof(AllUsers));
        }

        // GET: /AdminBoard/AllOlympiads (только для администратора)
        [HttpGet]
        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> AllOlympiads()
        {
            var olympiads = await GetAllOlympiadsForAdminAsync();
            return View(olympiads);
        }

        //TODO: Убрать заглушки и переработать согласно связи с БД
        #region Вспомогательные методы
        private async Task<AdminStatisticsViewModel> GetAdminStatisticsAsync()
        {
            await Task.Delay(1);
            return new AdminStatisticsViewModel
            {
                TotalUsers = 156,
                TotalOlympiads = 12,
                TotalParticipations = 342,
                ActiveOlympiads = 5,
                CompletedOlympiads = 7,
                OlympiadStats = new List<OlympiadStatisticsViewModel>
                {
                    new OlympiadStatisticsViewModel
                    {
                        OlympiadId = 1,
                        Title = "Олимпиада по математике",
                        ParticipantsCount = 45,
                        CompletedCount = 38,
                        Status = "Активна"
                    },
                    new OlympiadStatisticsViewModel
                    {
                        OlympiadId = 2,
                        Title = "Олимпиада по программированию",
                        ParticipantsCount = 67,
                        CompletedCount = 52,
                        Status = "Активна"
                    },
                    new OlympiadStatisticsViewModel
                    {
                        OlympiadId = 3,
                        Title = "Олимпиада по физике",
                        ParticipantsCount = 34,
                        CompletedCount = 34,
                        Status = "Завершена"
                    }
                }
            };
        }

        private async Task<List<UserAdminViewModel>> GetAllUsersAsync()
        {
            await Task.Delay(1);
            return new List<UserAdminViewModel>
            {
                new UserAdminViewModel
                {
                    UserId = 1,
                    Login = "admin",
                    FullName = "Администратор Системы",
                    Email = "admin@olympiad.ru",
                    Role = UserRoles.admin,
                    IsActive = true,
                    RegisteredAt = new DateTime(2024, 1, 15)
                },
                new UserAdminViewModel
                {
                    UserId = 2,
                    Login = "petrov_teacher",
                    FullName = "Петров Пётр Петрович",
                    Email = "petrov@school.ru",
                    Role = UserRoles.staff,
                    IsActive = true,
                    RegisteredAt = new DateTime(2024, 2, 10)
                },
                new UserAdminViewModel
                {
                    UserId = 3,
                    Login = "ivanov_ivan",
                    FullName = "Иванов Иван Иванович",
                    Email = "ivanov@student.ru",
                    Role = UserRoles.participant,
                    IsActive = true,
                    RegisteredAt = new DateTime(2024, 3, 5)
                },
                new UserAdminViewModel
                {
                    UserId = 4,
                    Login = "sidorova_maria",
                    FullName = "Сидорова Мария Сергеевна",
                    Email = "sidorova@student.ru",
                    Role = UserRoles.participant,
                    IsActive = false,
                    RegisteredAt = new DateTime(2024, 3, 20)
                }
            };
        }

        private async Task<EditUserByAdminViewModel?> GetUserForEditAsync(int userId)
        {
            await Task.Delay(1);
            var users = await GetAllUsersAsync();
            var user = users.FirstOrDefault(u => u.UserId == userId);

            if (user == null) return null;

            return new EditUserByAdminViewModel
            {
                UserId = user.UserId,
                Login = user.Login,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive,
                AvailableRoles = UserRoles.AllRoles
            };
        }

        private async Task<List<OlympiadCardViewModel>> GetAllOlympiadsForAdminAsync()
        {
            await Task.Delay(1);
            return new List<OlympiadCardViewModel>
            {
                new OlympiadCardViewModel
                {
                    OlympiadId = 1,
                    Title = "Олимпиада по математике",
                    Description = "Международная олимпиада по математике",
                    ImageUrl = "/images/math-olympiad.jpg",
                    EventStart = new DateTime(2025, 4, 15),
                    EventEnd = new DateTime(2025, 4, 20),
                    RegistOpen = new DateTime(2025, 3, 1),
                    RegistClosed = new DateTime(2025, 4, 10),
                    Status = "Регистрация открыта",
                    IsUserRegistered = false
                },
                new OlympiadCardViewModel
                {
                    OlympiadId = 2,
                    Title = "Олимпиада по программированию",
                    Description = "Всероссийская олимпиада по программированию",
                    ImageUrl = "/images/programming-olympiad.jpg",
                    EventStart = new DateTime(2025, 5, 10),
                    EventEnd = new DateTime(2025, 5, 15),
                    RegistOpen = new DateTime(2025, 4, 1),
                    RegistClosed = new DateTime(2025, 5, 5),
                    Status = "Регистрация открыта",
                    IsUserRegistered = false
                },
                new OlympiadCardViewModel
                {
                    OlympiadId = 3,
                    Title = "Олимпиада по физике",
                    Description = "Региональная олимпиада по физике",
                    ImageUrl = "/images/physics-olympiad.jpg",
                    EventStart = new DateTime(2025, 3, 20),
                    EventEnd = new DateTime(2025, 3, 25),
                    RegistOpen = new DateTime(2025, 2, 15),
                    RegistClosed = new DateTime(2025, 3, 15),
                    Status = "Завершена",
                    IsUserRegistered = false
                }
            };
        }
        #endregion

    }
}
