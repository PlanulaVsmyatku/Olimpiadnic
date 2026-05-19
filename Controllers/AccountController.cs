using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Olimpiadnic.Data;
using Olimpiadnic.Entities;
using Olimpiadnic.Models;
using Olimpiadnic.Models.AccountModels;
using Olimpiadnic.Models.RoleModels;
using Olimpiadnic.Services;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Olimpiadnic.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IInviteService _inviteService;
        private readonly IPasswordService _passwordService;

        // TODO -> EmailSender
        // private readonly IEmailSender _emailSender;

        public AccountController(IWebHostEnvironment webHostEnvironment, AppDbContext context, IInviteService inviteService,
            IPasswordService passwordService)
        {
            _webHostEnvironment = webHostEnvironment;
            _context = context;
            _inviteService = inviteService;
            _passwordService = passwordService;
        }

        // Вспомогательные DTO
        private class UserDto
        {
            public required int Id { get; set; }
            public required string Login { get; set; }
            public required string FullName { get; set; }
            public required string Email { get; set; }
            public required string Role { get; set; }
        }
        private class adminDto : UserDto
        {

        }
        private class staffDto : UserDto
        {
            public required string Phone { get; set; }
            public required string EducationalInstitution { get; set; }
            public required string Departament { get; set; }
            public required string City { get; set; }

        }
        private class participantDto : UserDto
        {
            public required string EducationLevel { get; set; }
            public required string City { get; set; }
            public required string EducationalInstitution { get; set; }
            public required string Curator { get; set; }
            public bool isActivated { get; set; }
        }

        #region Форма входа
        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            // Если пользователь уже авторизован, не показываем форму входа
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {

                // Проверка пользователя в БД
                var (succes, userDto) = await TryAuthenticateUser(model.LoginOrEmail, model.Password);

                if (succes)
                {
                    // Создание claims (данных пользователя)
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, userDto.Id.ToString()),
                        new Claim(ClaimTypes.Name, userDto.Login),
                        new Claim("FullName", userDto.FullName ?? ""),
                        new Claim(ClaimTypes.Email, userDto.Email ?? ""),
                        new Claim(ClaimTypes.Role, userDto.Role)
                    };

                    // Добавляем специфичные claims в зависимости от типа
                    switch (userDto)
                    {
                        case adminDto admin:
                            // Admin не имеет дополнительных полей
                            break;

                        case staffDto staff:
                            
                            claims.Add(new Claim("Phone", staff.Phone ?? ""));
                            claims.Add(new Claim("EducationalInstitution", staff.EducationalInstitution ?? ""));
                            claims.Add(new Claim("Department", staff.Departament ?? ""));
                            claims.Add(new Claim("City", staff.City ?? ""));
                            break;

                        case participantDto participant:
                            claims.Add(new Claim("EducationLevel", participant.EducationLevel ?? ""));
                            claims.Add(new Claim("City", participant.City ?? ""));
                            claims.Add(new Claim("EducationalInstitution", participant.EducationalInstitution ?? ""));
                            claims.Add(new Claim("Curator", participant.Curator ?? ""));
                            claims.Add(new Claim("IsActivated", participant.isActivated.ToString()));
                            break;
                    }

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe, // Запомнить пользователя
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                    };

                    // Вход пользователя
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // Перенаправление на запрошенную страницу или на главную
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Неверный логин или пароль.");
            }

            return View(model);
        }

        // GET: /Account/Logout
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            // Выход из системы
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Показываем страницу с таймером
            return View("Logout");
        }

        // Вспомогательный метод для проверки пользователя 
        /// <summary>
        /// Проверяет существование пользователя и возвращает DTO в зависимости от роли
        /// </summary>
        /// <param name="loginOrEmail"> Логин или почта</param>
        /// <param name="password"> Пароль который захешуется</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private async Task<(bool success, UserDto user)> TryAuthenticateUser(string loginOrEmail, string password)
        {

            // 1. Найти пользователя по логину
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Login == loginOrEmail);

            // 2. Если не нашли, ищем по email через UserProfile
            if (user == null)
            {
                var profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.Email == loginOrEmail);

                if (profile != null)
                {
                    user = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserId == profile.UserId);
                }
            }

            if (user != null)
            {
                // 2. Проверить хеш пароля
                if (!_passwordService.VerifyPassword(password, user.PasswordHash))
                {
                    return (false, null);
                }


                var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.UserId);

                
                // Ищем Table2 через Table1
                var userRoles = await _context.UserRoles
                    .FirstOrDefaultAsync(t2 => t2.UserId == user.UserId);

                // Получаем Table3
                var userRole = await _context.Roles
                    .FirstOrDefaultAsync(t3 => t3.RoleId == userRoles.RoleId);

                UserDto userDto = userRole.Name switch
                {
                    "admin" => new adminDto
                    {
                        Id = user.UserId,
                        Login = user.Login,
                        FullName = userProfile.FullName,
                        Role = userRole.Name,
                        Email = userProfile.Email
                    },

                    "staff" => new staffDto
                    {
                        Id = user.UserId,
                        Login = user.Login,
                        FullName = userProfile.FullName,
                        Role = userRole.Name,
                        Email = userProfile.Email,
                        Phone = userProfile.Phone,
                        EducationalInstitution = userProfile.Organisation,
                        Departament = userProfile.PositionGrade,
                        City = userProfile.City
                    },

                    "participant" => new participantDto
                    {
                        Id = user.UserId,
                        Login = user.Login,
                        FullName = userProfile.FullName,
                        Role = userRole.Name,
                        Email = userProfile.Email,
                        EducationLevel = userProfile.PositionGrade,
                        City = userProfile.City,
                        EducationalInstitution = userProfile.Organisation,
                        Curator = userProfile.Kurator,
                        isActivated = user.IsActivated
                    },

                    _ => throw new ArgumentException($"Неизвестная роль: {userRole.Name}")
                };

                return (true, userDto);
            }
            // 3. Вернуть пользователя или null
            return (false, null);
        }
        #endregion

        #region Форма регистрации
        // GET: /Account/Register - для участников
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            // Если пользователь уже зарегестрирован, не показываем форму входа
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
        // POST: /Account/Register - для участников
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>Register(ParticipantRegisterViewModel model)
        {
            // ВАЖНО: Проверка подтверждения пароля происходит здесь автоматически
            // Атрибут [Compare] в модели уже добавил ошибку в ModelState, если пароли не совпадают

            if (ModelState.IsValid)
            {
                #region Общая проверка - пароль, сила пароля, существует ли логин/почта (в теории заменить отдельным классом)
                // Дополнительная проверка силы пароля
                var (isValid, message) = _passwordService.ValidatePasswordStrength(model.Password);
                if (!isValid)
                {
                    ModelState.AddModelError("Password", message);
                    return View(model);
                }

                // Проверка: существует ли пользователь с таким логином или email
                if (await UserExists(model.Login, model.Email))
                {
                    if (await LoginExists(model.Login))
                        ModelState.AddModelError("Login", "Пользователь с таким логином уже существует");

                    if (await EmailExists(model.Email))
                        ModelState.AddModelError("Email", "Пользователь с таким email уже зарегистрирован");

                    return View(model);
                }
                #endregion
                
                // Сохранение файла согласия
                string consentFilePath = null;
                if (model.ConsentFile != null && model.ConsentFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "consents");
                    Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = $"{Guid.NewGuid()}_{model.ConsentFile.FileName}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ConsentFile.CopyToAsync(fileStream);
                    }

                    consentFilePath = uniqueFileName;
                }

                //Здесь тоже нужно вывести код отдельно, т.к повторяется в методе регистрации сотрудника
                // Сохранение пользователя в базу данных
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Хеширование пароля перед сохранением
                    // 1. Сначала создаем пользователя (без связанных данных)
                    var hashedPassword = _passwordService.HashPassword(model.Password);

                    var user = new User
                    {
                        Login = model.Login,
                        PasswordHash = hashedPassword,
                        CreatedAt = DateTime.UtcNow,
                        IsActivated = false,
                        LastLogin = null
                    };

                    // Добавляем пользователя в БД
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync(); // Сохраняем, чтобы получить User_ID

                    // 2. Теперь создаем профиль пользователя, используя полученный User_ID
                    var userProfile = new UserProfile
                    {
                        UserId = user.UserId, // Внешний ключ к Users
                        FullName = model.FullName,
                        Kurator = model.Curator,
                        Email = model.Email,
                        Organisation = model.EducationalInstitution, // если есть
                        PositionGrade = model.EducationLevel, // если есть
                        City = model.City,
                        ConsentFileUrl = consentFilePath
                    };

                    _context.UserProfiles.Add(userProfile);

                    // 3. Назначаем роль пользователю
                    var userRole = new UserRole
                    {
                        UserId = user.UserId,
                        RoleId = 1 // Участник
                    };

                    _context.UserRoles.Add(userRole);

                    // 4. Сохраняем все связанные данные
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // 5. Перенаправляем на страницу успеха
                    TempData["SuccessMessage"] = "Регистрация прошла успешно!";
                    return RedirectToAction("RegisterSuccess");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", $"Ошибка при регистрации: {ex.Message}");
                    return View(model);
                }
            }

            // Если модель невалидна, возвращаем форму с ошибками
            return View(model);
        }

        // === регистрация сотрудника по инвайт ссылке ===
        // GET: /Account/StaffRegister - для сотрудников/админов
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult>StaffRegister(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Index", "Home");
            }
            // проверяем валидность токена
            var isValid = await _inviteService.ValidateInviteToken(token);

            if (!isValid)
            {
                ViewBag.Error = "Приглашение недействительно или срок его действия истёк";
                return RedirectToAction("Index", "Home");
            }

            var email = await _inviteService.GetInviteEmail(token);

            // Используем форму-модель для GET запроса
            var model = new StaffRegisterFormModel
            {
                InviteToken = token,
                Email = email
            };

            return View(model);
        }

        // POST: /Account/StaffRegister - для сотрудников/админа
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>StaffRegister(StaffRegisterViewModel model)
        {
            // ВАЖНО: Проверка подтверждения пароля происходит здесь автоматически
            // Атрибут [Compare] в модели уже добавил ошибку в ModelState, если пароли не совпадают

            if (ModelState.IsValid)
            {
                // Дополнительная проверка токена
                var isValidToken = await _inviteService.ValidateInviteToken(model.InviteToken);
                if (!isValidToken)
                {
                    ViewBag.Error = "Приглашение недействительно или срок его действия истёк";
                    return RedirectToAction("Index", "Home");
                }

                #region Общая проверка - пароль, сила пароля, существует ли логин/почта (в теории заменить отдельным классом)
                // Дополнительная проверка силы пароля
                var (isValid, message) = _passwordService.ValidatePasswordStrength(model.Password);
                if (!isValid)
                { 
                    return BadRequest(new { error = message });
                }


                // Проверка: существует ли пользователь с таким логином или email
                if (await UserExists(model.Login, model.Email))
                {
                    if (await LoginExists(model.Login))
                        ModelState.AddModelError("Login", "Пользователь с таким логином уже существует");

                    if (await EmailExists(model.Email))
                        ModelState.AddModelError("Email", "Пользователь с таким email уже зарегистрирован");

                    return View(model);
                }
                #endregion

                // Сохранение пользователя в базу данных
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Хеширование пароля перед сохранением
                    // 1. Сначала создаем пользователя (без связанных данных)
                    var hashedPassword = _passwordService.HashPassword(model.Password);

                    var user = new User
                    {
                        Login = model.Login,
                        PasswordHash = hashedPassword,
                        CreatedAt = DateTime.UtcNow,
                        IsActivated = false,
                        LastLogin = null
                    };

                    // Добавляем пользователя в БД
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync(); // Сохраняем, чтобы получить User_ID

                    // 2. Теперь создаем профиль пользователя, используя полученный User_ID
                    var userProfile = new UserProfile
                    {
                        UserId = user.UserId, // Внешний ключ к Users
                        FullName = model.FullName,
                        Phone = model.Phone,
                        Email = model.Email,
                        Organisation = "КузТАГис", // если есть
                        PositionGrade = model.Department, // если есть
                        City = "Кузбасс, Кемерово"
                    };

                    _context.UserProfiles.Add(userProfile);

                    // 3. Назначаем роль пользователю
                    var userRole = new UserRole 
                    {
                        UserId = user.UserId,
                        RoleId = 2 // Сотрудник
                    };

                    _context.UserRoles.Add(userRole);

                    // 4. Сохраняем все связанные данные
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // 5. Используем токен приглашения
                    await _inviteService.UseInviteToken(model.InviteToken);

                    // 6. Перенаправляем на страницу успеха
                    TempData["SuccessMessage"] = "Регистрация прошла успешно!";
                    return RedirectToAction("RegisterSuccess");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", $"Ошибка при регистрации: {ex.Message}");
                    return View(model);
                }

                
            }

            // Если модель невалидна, возвращаем форму с ошибками
            return View(model);
        }


        // Вспомогательные методы
        private async Task<bool> UserExists(string login, string email)
        {
            // Проверка в базе данных
            return await _context.Users.AnyAsync(u => u.Login == login) || await _context.UserProfiles.AnyAsync(p => p.Email == email);

        }

        private async Task<bool> LoginExists(string login)
        {
            // Проверка в базе данных
            return await _context.Users.AnyAsync((User u) => u.Login == login);
            
        }

        private async Task<bool> EmailExists(string email)
        {
            // TODO: Проверка в базе данных
             return await _context.UserProfiles.AnyAsync(p => p.Email == email);
            
        }

        [AllowAnonymous]
        public IActionResult RegisterSuccess()
        {
            return View();
        }
        #endregion

        #region AccesDenied
        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
        #endregion
    }
}
