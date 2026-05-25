using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Olimpiadnic.Data;
using Olimpiadnic.Models.MyOlympiads;
using Olimpiadnic.Models.OlympiadModels;
using Olimpiadnic.Models.RoleModels;
using Olimpiadnic.Services;
using Olimpiadnic.Services.Repos;
using System.Security.Claims;

namespace Olimpiadnic.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ILogger<ProfileController> _logger;
        private readonly AppDbContext _context;
        private readonly IOlympiadRepository _olympiadRepository;
        private readonly IPasswordService _passwordService;

        public ProfileController(
            ILogger<ProfileController> logger,
            AppDbContext context,
            IPasswordService passwordService,
            IOlympiadRepository olympiadRepository)
        {
            _logger = logger;
            _context = context;
            _olympiadRepository = olympiadRepository;
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

        [HttpGet]
        public async Task<IActionResult> MyOlympiads(MyOlympiadsFilterViewModel? filter, int page = 1)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
            var viewModel = new MyOlympiadsViewModel
            {
                Filter = filter ?? new MyOlympiadsFilterViewModel(),
                CurrentPage = page,
                PageSize = 6
            };

            try
            {
                if (role == "participant")
                {
                    var result = await _olympiadRepository.GetParticipantOlympiadsAsync(
                        userId, viewModel.Filter, page, viewModel.PageSize);
                    viewModel.ParticipantOlympiads = result.Items;
                    viewModel.CurrentPage = result.CurrentPage;
                    viewModel.TotalPages = result.TotalPages;
                    viewModel.TotalCount = result.TotalCount;
                    viewModel.PageSize = result.PageSize;
                }
                else if (role == "staff")
                {
                    var result = await _olympiadRepository.GetStaffOlympiadsAsync(
                        userId, viewModel.Filter, page, viewModel.PageSize);
                    viewModel.StaffOlympiads = result.Items;
                    viewModel.CurrentPage = result.CurrentPage;
                    viewModel.TotalPages = result.TotalPages;
                    viewModel.TotalCount = result.TotalCount;
                    viewModel.PageSize = result.PageSize;
                }
                else if (role == "admin")
                {
                    var result = await _olympiadRepository.GetAllOlympiadsForAdminAsync(
                        viewModel.Filter, page, 10);
                    viewModel.AdminOlympiads = result.Items;
                    viewModel.CurrentPage = result.CurrentPage;
                    viewModel.TotalPages = result.TotalPages;
                    viewModel.TotalCount = result.TotalCount;
                    viewModel.PageSize = result.PageSize;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке страницы Мои олимпиады для роли {Role}", role);
                TempData["Error"] = "Произошла ошибка при загрузке данных";
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOlympiad(int id)
        {
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
            if (role != "admin")
            {
                return Json(new { success = false, message = "Недостаточно прав" });
            }

            try
            {
                var olympiad = await _olympiadRepository.GetOlympiadByIdAsync(id);
                if (olympiad == null)
                {
                    return Json(new { success = false, message = "Олимпиада не найдена" });
                }

                // TODO: Удаление олимпиады (можно мягкое удаление или каскадное)
                // _context.Olympiads.Remove(olympiad);
                // await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Олимпиада удалена" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении олимпиады {OlympiadId}", id);
                return Json(new { success = false, message = "Ошибка при удалении" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOlympiadField([FromBody] UpdateOlympiadFieldRequest request)
        {
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
            if (role != "admin")
            {
                return Json(new { success = false, message = "Недостаточно прав" });
            }

            try
            {
                var olympiad = await _olympiadRepository.GetOlympiadByIdAsync(request.OlympiadId);
                if (olympiad == null)
                {
                    return Json(new { success = false, message = "Олимпиада не найдена" });
                }

                // Обновляем соответствующее поле
                switch (request.Field.ToLower())
                {
                    case "title":
                        olympiad.Title = request.Value;
                        break;
                    case "description":
                        olympiad.Description = request.Value;
                        break;
                    case "credentials":
                        olympiad.Credentials = request.Value;
                        break;
                    case "status":
                        olympiad.Status = request.Value;
                        break;
                    case "eventstart":
                        if (DateTime.TryParse(request.Value, out DateTime eventStart))
                            olympiad.EventStart = eventStart;
                        else
                            return Json(new { success = false, message = "Неверный формат даты" });
                        break;
                    case "eventend":
                        if (DateTime.TryParse(request.Value, out DateTime eventEnd))
                            olympiad.EventEnd = eventEnd;
                        else
                            return Json(new { success = false, message = "Неверный формат даты" });
                        break;
                    case "registopen":
                        if (DateTime.TryParse(request.Value, out DateTime registOpen))
                            olympiad.RegistOpen = registOpen;
                        else
                            return Json(new { success = false, message = "Неверный формат даты" });
                        break;
                    case "registclosed":
                        if (DateTime.TryParse(request.Value, out DateTime registClosed))
                            olympiad.RegistClosed = registClosed;
                        else
                            return Json(new { success = false, message = "Неверный формат даты" });
                        break;
                    default:
                        return Json(new { success = false, message = "Неизвестное поле" });
                }

                await _olympiadRepository.UpdateOlympiadAsync(olympiad);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении поля {Field} олимпиады {OlympiadId}", request.Field, request.OlympiadId);
                return Json(new { success = false, message = "Произошла ошибка при сохранении" });
            }
        }

        // DTO для запроса обновления
        public class UpdateOlympiadFieldRequest
        {
            public int OlympiadId { get; set; }
            public string Field { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }

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