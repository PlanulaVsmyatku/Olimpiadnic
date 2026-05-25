using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Olimpiadnic.Data;
using Olimpiadnic.Models.OlympiadModels;
using Olimpiadnic.Models.RoleModels;
using Olimpiadnic.Services;
using System.Security.Claims;

namespace Olimpiadnic.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ILogger<ProfileController> _logger;
        private readonly AppDbContext _context;
        private readonly IPasswordService _passwordService;

        public ProfileController(
            ILogger<ProfileController> logger,
            AppDbContext context,
            IPasswordService passwordService)
        {
            _logger = logger;
            _context = context;
            _passwordService = passwordService;
        }

        // GET: /Profile/Profile (страница по умолчанию в личном кабинете)
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role) ?? UserRoles.participant;
            
            // Данные уже загружены в DTO на этапе входа и теперь в claims
            var profile = await GetUserProfile();
            
            ViewBag.UserRole = role;
            ViewBag.UserID = userId;
            return View(profile);
        }

        // POST: /Profile/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile([FromForm] IFormCollection form)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                var role = User.FindFirstValue(ClaimTypes.Role) ?? UserRoles.participant;

                // Получаем пользователя и его профиль из БД
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

                if (user == null || userProfile == null)
                {
                    return Json(new { success = false, message = "Пользователь не найден" });
                }

                // Обновляем общие поля
                userProfile.FullName = form["FullName"].ToString();
                userProfile.Email = form["Email"].ToString();

                // Обновляем поля в зависимости от роли
                switch (role)
                {
                    case "participant":
                        userProfile.City = form["City"].ToString();
                        userProfile.PositionGrade = form["EducationLevel"].ToString();
                        userProfile.Kurator = form["Curator"].ToString();
                        userProfile.Organisation = form["EducationalInstitution"].ToString();
                        break;

                    case "staff":
                        userProfile.Phone = form["PhoneNumber"].ToString();
                        userProfile.City = form["City"].ToString();
                        userProfile.PositionGrade = form["Departament"].ToString();
                        userProfile.Organisation = form["EducationalInstitution"].ToString();
                        break;

                    case "admin":
                        // Админ не имеет дополнительных полей
                        break;
                }

                await _context.SaveChangesAsync();

                // Обновляем claims пользователя
                await UpdateUserClaims(userId);

                return Json(new { success = true, message = "Профиль успешно обновлён" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                return Json(new { success = false, message = "Произошла ошибка при обновлении профиля" });
            }
        }

        // GET: /Profile/MyOlympiads (для участника - записанные, для сотрудника - созданные)
        /*
        [HttpGet]
        public async Task<IActionResult> MyOlympiads()
        {
            var role = User.FindFirstValue(ClaimTypes.Role) ?? UserRoles.participant;
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            
            if (role == UserRoles.participant)
            {
                var enrolledOlympiads = await GetEnrolledOlympiads(userId);
                ViewBag.Role = "Participant";
                return View("MyOlympiads_Participant", enrolledOlympiads);
            }
            else if (role == UserRoles.staff || role == UserRoles.admin)
            {
                var createdOlympiads = await GetCreatedOlympiads(userId);
                ViewBag.Role = "Staff";
                return View("MyOlympiads_Staff", createdOlympiads);
            }
            
            return RedirectToAction(nameof(Profile));
        }

        // GET: /Profile/CompletedOlympiads (только для участника)
        [HttpGet]
        public async Task<IActionResult> CompletedOlympiads()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var completed = await GetCompletedOlympiads(userId);
            return View(completed);
        }


        // GET: /Profile/GetParticipantResults (для модального окна)
        [HttpGet]
        public async Task<IActionResult> GetParticipantResults(int participantId, int olympiadId)
        {
            // TODO: Получить результаты участника
            var results = await GetParticipantResultsFromDb(participantId, olympiadId);
            return PartialView("_ParticipantResultsModal", results);
        }

        // GET: /Profile/GetOlympiadParticipants (для сотрудника)
        [HttpGet]
        public async Task<IActionResult> GetOlympiadParticipants(int olympiadId)
        {
            var participants = await GetOlympiadParticipantsFromDb(olympiadId);
            return PartialView("_OlympiadParticipantsModal", participants);
        }

        // POST: /Profile/RequestDeleteOlympiad (для сотрудника)
        [HttpPost]
        public async Task<IActionResult> RequestDeleteOlympiad(int olympiadId)
        {
            // TODO: Отправить запрос администратору на удаление
            TempData["SuccessMessage"] = "Запрос на удаление отправлен администратору";
            return RedirectToAction(nameof(MyOlympiads));
        }
        */
        #region Вспомогательные методы

        private async Task<UserProfileViewModel> GetUserProfile()
        {
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "participant";

            UserProfileViewModel UserProfile = role switch
            {
                "participant" => new ParticipantProfileViewModel
                {
                    Login = User.FindFirstValue(ClaimTypes.Name),
                    FullName = User.FindFirstValue("FullName"),
                    Role = role,
                    Email = User.FindFirstValue(ClaimTypes.Email),
                    EducationLevel = User.FindFirstValue("EducationLevel"),
                    City = User.FindFirstValue("City"),
                    EducationalInstitution = User.FindFirstValue("EducationalInstitution"),
                    Curator = User.FindFirstValue("Curator"),
                    IsActive = bool.TryParse(User.FindFirstValue("IsActivated"), out bool isActive) && isActive
                },

                "staff" => new StaffProfileViewModel
                {
                    Login = User.FindFirstValue(ClaimTypes.Name),
                    FullName = User.FindFirstValue("FullName"),
                    Role = role,
                    Email = User.FindFirstValue(ClaimTypes.Email),
                    PhoneNumber = User.FindFirstValue("Phone") ?? "",
                    City = User.FindFirstValue("City") ?? "",
                    EducationalInstitution = User.FindFirstValue("EducationalInstitution") ?? "",
                    Departament = User.FindFirstValue("Departament") ?? User.FindFirstValue("Department") ?? ""
                },

                "admin" => new AdminProfileViewModel
                {
                    Login = User.FindFirstValue(ClaimTypes.Name),
                    FullName = User.FindFirstValue("FullName"),
                    Email = User.FindFirstValue(ClaimTypes.Email),
                    Role = role
                },

                _ => throw new ArgumentException($"Неизвестная роль: {role}")
            };

            return UserProfile;
        }

        private async Task UpdateUserClaims(int userId)
        {
            // Получаем свежие данные из БД
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return;

            var userProfile = user.UserProfile;
            var role = user.UserRoles?.FirstOrDefault()?.Role?.Name ?? "participant";

            // Создаём новые claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Login),
                new Claim("FullName", userProfile?.FullName ?? ""),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Email, userProfile?.Email ?? "")
            };

            // Добавляем специфичные claims в зависимости от роли
            if (role == "participant" && userProfile != null)
            {
                claims.Add(new Claim("EducationLevel", userProfile.PositionGrade ?? ""));
                claims.Add(new Claim("City", userProfile.City ?? ""));
                claims.Add(new Claim("EducationalInstitution", userProfile.Organisation ?? ""));
                claims.Add(new Claim("Curator", userProfile.Kurator ?? ""));
                claims.Add(new Claim("IsActivated", user.IsActivated.ToString()));
            }
            else if (role == "staff" && userProfile != null)
            {
                claims.Add(new Claim("Phone", userProfile.Phone ?? ""));
                claims.Add(new Claim("City", userProfile.City ?? ""));
                claims.Add(new Claim("EducationalInstitution", userProfile.Organisation ?? ""));
                claims.Add(new Claim("Departament", userProfile.PositionGrade ?? ""));
            }

            // Обновляем аутентификацию
            await HttpContext.SignOutAsync();
            var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
            await HttpContext.SignInAsync(new ClaimsPrincipal(claimsIdentity));
        }

        private string GetRoleDisplayName(string role)
        {
            return role switch
            {
                "participant" => "Участник",
                "staff" => "Сотрудник",
                "admin" => "Администратор",
                _ => "Роль?"
            };
        }
        #endregion

        #region Мои олимпиады 




        #endregion
    }
}