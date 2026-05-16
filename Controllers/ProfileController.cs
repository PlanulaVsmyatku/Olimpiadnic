using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Olimpiadnic.Models.OlympiadModels;
using Olimpiadnic.Models.RoleModels;
using System.Security.Claims;

namespace Olimpiadnic.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(ILogger<ProfileController> logger)
        {
            _logger = logger;
        }

        // GET: /Dashboard/Profile (страница по умолчанию в личном кабинете)
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role) ?? UserRoles.Participant;
            
            // TODO: Загрузить данные пользователя из БД
            var profile = await GetUserProfile(int.Parse(userId));
            
            ViewBag.UserRole = role;
            return View(profile);
        }

        // GET: /Dashboard/EditProfile
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var profile = await GetUserProfile(int.Parse(userId));
            
            var editModel = new EditProfileViewModel
            {
                Login = profile.Login,
                FullName = profile.FullName,
                Email = profile.Email,
                PhoneNumber = profile.PhoneNumber,
                EducationalInstitution = profile.EducationalInstitution,
                EducationLevel = profile.EducationLevel,
                Position = profile.Position
            };
            
            return View(editModel);
        }

        // POST: /Dashboard/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // TODO: Сохранить изменения в БД
            
            TempData["SuccessMessage"] = "Профиль успешно обновлён!";
            return RedirectToAction(nameof(Profile));
        }

        // GET: /Dashboard/MyOlympiads (для участника - записанные, для сотрудника - созданные)
        [HttpGet]
        public async Task<IActionResult> MyOlympiads()
        {
            var role = User.FindFirstValue(ClaimTypes.Role) ?? UserRoles.Participant;
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            
            if (role == UserRoles.Participant)
            {
                var enrolledOlympiads = await GetEnrolledOlympiads(userId);
                ViewBag.Role = "Participant";
                return View("MyOlympiads_Participant", enrolledOlympiads);
            }
            else if (role == UserRoles.Staff || role == UserRoles.Admin)
            {
                var createdOlympiads = await GetCreatedOlympiads(userId);
                ViewBag.Role = "Staff";
                return View("MyOlympiads_Staff", createdOlympiads);
            }
            
            return RedirectToAction(nameof(Profile));
        }

        // GET: /Dashboard/CompletedOlympiads (только для участника)
        [HttpGet]
        public async Task<IActionResult> CompletedOlympiads()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var completed = await GetCompletedOlympiads(userId);
            return View(completed);
        }


        // GET: /Dashboard/GetParticipantResults (для модального окна)
        [HttpGet]
        public async Task<IActionResult> GetParticipantResults(int participantId, int olympiadId)
        {
            // TODO: Получить результаты участника
            var results = await GetParticipantResultsFromDb(participantId, olympiadId);
            return PartialView("_ParticipantResultsModal", results);
        }

        // GET: /Dashboard/GetOlympiadParticipants (для сотрудника)
        [HttpGet]
        public async Task<IActionResult> GetOlympiadParticipants(int olympiadId)
        {
            var participants = await GetOlympiadParticipantsFromDb(olympiadId);
            return PartialView("_OlympiadParticipantsModal", participants);
        }

        // POST: /Dashboard/RequestDeleteOlympiad (для сотрудника)
        [HttpPost]
        public async Task<IActionResult> RequestDeleteOlympiad(int olympiadId)
        {
            // TODO: Отправить запрос администратору на удаление
            TempData["SuccessMessage"] = "Запрос на удаление отправлен администратору";
            return RedirectToAction(nameof(MyOlympiads));
        }

        #region Вспомогательные методы (заглушки для БД)

        private async Task<UserProfileViewModel> GetUserProfile(int userId)
        {
            await Task.Delay(1);
            var role = User.FindFirstValue(ClaimTypes.Role) ?? UserRoles.Participant;
            
            return new UserProfileViewModel
            {
                Login = "ivanov_ivan",
                FullName = "Иванов Иван Иванович",
                Email = "ivanov@example.com",
                PhoneNumber = "+7 (123) 456-78-90",
                EducationalInstitution = "МГУ им. Ломоносова",
                EducationLevel = role == UserRoles.Participant ? "Высшее" : null,
                Position = role != UserRoles.Participant ? "Старший преподаватель" : null,
                IsActive = true,
                Role = role
            };
        }

        private async Task<List<OlympiadCardViewModel>> GetEnrolledOlympiads(int userId)
        {
            await Task.Delay(1);
            return new List<OlympiadCardViewModel>
            {
                new OlympiadCardViewModel
                {
                    OlympiadId = 1,
                    Title = "Олимпиада по математике",
                    Description = "Международная олимпиада по математике для школьников 9-11 классов",
                    ImageUrl = "/images/math-olympiad.jpg",
                    EventStart = new DateTime(2025, 5, 15, 10, 0, 0),
                    EventEnd = new DateTime(2025, 5, 20, 18, 0, 0),
                    RegistOpen = DateTime.Now.AddDays(-10),
                    RegistClosed = DateTime.Now.AddDays(5),
                    Status = "Регистрация открыта",
                    IsUserRegistered = true
                },
                new OlympiadCardViewModel
                {
                    OlympiadId = 2,
                    Title = "Олимпиада по программированию",
                    Description = "Всероссийская олимпиада по программированию",
                    ImageUrl = "/images/programming-olympiad.jpg",
                    EventStart = new DateTime(2025, 6, 10, 9, 0, 0),
                    EventEnd = new DateTime(2025, 6, 15, 20, 0, 0),
                    RegistOpen = DateTime.Now.AddDays(-5),
                    RegistClosed = DateTime.Now.AddDays(10),
                    Status = "Регистрация открыта",
                    IsUserRegistered = true
                }
            };
        }

        private async Task<List<StaffOlympiadCardViewModel>> GetCreatedOlympiads(int userId)
        {
            await Task.Delay(1);
            return new List<StaffOlympiadCardViewModel>
            {
                new StaffOlympiadCardViewModel
                {
                    OlympiadId = 1,
                    Title = "Олимпиада по программированию",
                    Description = "Всероссийская олимпиада по программированию 2025",
                    ImageUrl = "/images/programming-olympiad.jpg",
                    EventStart = new DateTime(2025, 6, 10, 9, 0, 0),
                    EventEnd = new DateTime(2025, 6, 15, 20, 0, 0),
                    ParticipantsCount = 45,
                    PendingManualChecks = 12
                },
                new StaffOlympiadCardViewModel
                {
                    OlympiadId = 2,
                    Title = "Олимпиада по физике",
                    Description = "Региональная олимпиада по физике",
                    ImageUrl = "/images/physics-olympiad.jpg",
                    EventStart = new DateTime(2025, 7, 20, 10, 0, 0),
                    EventEnd = new DateTime(2025, 7, 25, 18, 0, 0),
                    ParticipantsCount = 28,
                    PendingManualChecks = 5
                }
            };
        }

        private async Task<List<CompletedOlympiadViewModel>> GetCompletedOlympiads(int userId)
        {
            await Task.Delay(1);
            return new List<CompletedOlympiadViewModel>
            {
                new CompletedOlympiadViewModel
                {
                    OlympiadId = 1,
                    Title = "Олимпиада по математике (весенняя)",
                    CompletedAt = new DateTime(2025, 3, 20),
                    TotalScore = 85,
                    MaxScore = 100
                },
                new CompletedOlympiadViewModel
                {
                    OlympiadId = 2,
                    Title = "Олимпиада по информатике",
                    CompletedAt = new DateTime(2025, 2, 15),
                    TotalScore = 92,
                    MaxScore = 100
                }
            };
        }


        private async Task<object> GetParticipantResultsFromDb(int participantId, int olympiadId)
        {
            await Task.Delay(1);
            return new 
            { 
                ParticipantName = "Иванов Иван Иванович", 
                Score = 85, 
                MaxScore = 100, 
                Details = new List<object>
                {
                    new { Question = "Вопрос 1", Score = 25, MaxScore = 25, IsCorrect = true },
                    new { Question = "Вопрос 2", Score = 20, MaxScore = 25, IsCorrect = false },
                    new { Question = "Вопрос 3 (развёрнутый)", Score = 40, MaxScore = 50, IsCorrect = true }
                }
            };
        }

        private async Task<List<ParticipantResultViewModel>> GetOlympiadParticipantsFromDb(int olympiadId)
        {
            await Task.Delay(1);
            return new List<ParticipantResultViewModel>
            {
                new ParticipantResultViewModel
                {
                    ParticipantId = 1,
                    ParticipantName = "Иванов Иван Иванович",
                    ParticipantLogin = "ivanov_ivan",
                    Score = 85,
                    Status = "Завершено"
                },
                new ParticipantResultViewModel
                {
                    ParticipantId = 2,
                    ParticipantName = "Петрова Мария Сергеевна",
                    ParticipantLogin = "petrova_m",
                    Score = null,
                    Status = "В процессе"
                },
                new ParticipantResultViewModel
                {
                    ParticipantId = 3,
                    ParticipantName = "Сидоров Алексей Дмитриевич",
                    ParticipantLogin = "sidorov_a",
                    Score = 92,
                    Status = "Завершено"
                }
            };
        }

        #endregion
    }
}