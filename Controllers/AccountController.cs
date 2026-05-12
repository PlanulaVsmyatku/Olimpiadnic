using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Olimpiadnic.Models;
using Olimpiadnic.Models.AccountModels;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Olimpiadnic.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        [AllowAnonymous]
        public string Index()
        {
            return "Ответ по умолчанию";
        }

        private readonly IWebHostEnvironment _webHostEnvironment;

        // TODO -> EmailSender
        // private readonly IEmailSender _emailSender;

        public AccountController(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        // Вспомогательный DTO для пользователя
        private class UserDto
        {
            public required int Id { get; set; }
            public required string Login { get; set; }
            public required string Email { get; set; }
            public required string FullName { get; set; }
            public required string EducationLevel { get; set; }
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
                // TODO: Проверка пользователя в базе данных
                // var user = await _context.Users
                //     .FirstOrDefaultAsync(u => u.Login == model.LoginOrEmail || u.Email == model.LoginOrEmail);

                // Временная проверка для примера (заменить на реальную проверку в БД)
                var user = await AuthenticateUser(model.LoginOrEmail, model.Password);

                if (user != null)
                {
                    // Создание claims (данных пользователя)
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.Login),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim("FullName", user.FullName ?? ""),
                        new Claim("EducationLevel", user.EducationLevel ?? "")
                    };

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
        public async Task<IActionResult> Logout()
        {
            // Выход из системы
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Показываем страницу с таймером
            return View("Logout");
        }

        // Вспомогательный метод для проверки пользователя (временный)
        private async Task<UserDto> AuthenticateUser(string loginOrEmail, string password)
        {
            // TODO: Заменить на реальную проверку в базе данных
            // Пример имитации проверки
            await Task.Delay(1); // Имитация асинхронности

            // TODO:
            // 1. Найти пользователя в БД по loginOrEmail
            // 2. Проверить хеш пароля (BCrypt.Verify(password, user.PasswordHash))
            // 3. Вернуть пользователя или null

            // Временная заглушка для тестирования
            if (loginOrEmail == "admin@example.com" && password == "123456")
            {
                return new UserDto
                {
                    Id = 1,
                    Login = "admin",
                    Email = "admin@example.com",
                    FullName = "Администратор",
                    EducationLevel = "Высшее"
                };
            }

            return null;
        }

        [Authorize]
        public IActionResult Profile()
        {
            // Получение ID пользователя
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Получение логина
            var login = User.Identity.Name;

            // Получение email
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            // Получение кастомного поля
            var fullName = User.FindFirst("FullName")?.Value;

            // TODO: Загрузить пользователя из БД и отдать в представление
            return View();
        }

        
        #endregion

        #region Форма регистрации

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // ВАЖНО: Проверка подтверждения пароля происходит здесь автоматически
            // Атрибут [Compare] в модели уже добавил ошибку в ModelState, если пароли не совпадают

            if (ModelState.IsValid)
            {
                // Дополнительная проверка (на всякий случай, хотя Compare уже сработал)
                if (model.Password != model.ConfirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "Пароли не совпадают");
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

               // Хеширование пароля перед сохранением
               //string hashedPassword = HashPassword(model.Password);

                // TODO: Сохранение пользователя в базу данных
                // var user = new User
                // {
                //     Login = model.Login,
                //     FullName = model.FullName,
                //     City = model.City,
                //     Email = model.Email,
                //     PasswordHash = hashedPassword,
                //     EducationLevel = model.EducationLevel,
                //     EducationalInstitution = model.EducationalInstitution,
                //     Curator = model.Curator,
                //     ConsentFile = consentFilePath,
                //     RegisteredAt = DateTime.UtcNow
                // };
                // 
                // _context.Users.Add(user);
                // await _context.SaveChangesAsync();

                // Перенаправление на страницу успеха
                TempData["SuccessMessage"] = "Регистрация прошла успешно!";
                return RedirectToAction("RegisterSuccess");
            }

            // Если модель невалидна, возвращаем форму с ошибками
            return View(model);
        }

        // Вспомогательные методы
        private async Task<bool> UserExists(string login, string email)
        {
            // TODO: Проверка в базе данных
            // return await _context.Users.AnyAsync(u => u.Login == login || u.Email == email);
            return false;
        }

        private async Task<bool> LoginExists(string login)
        {
            // TODO: Проверка в базе данных
            // return await _context.Users.AnyAsync(u => u.Login == login);
            return false;
        }

        private async Task<bool> EmailExists(string email)
        {
            // TODO: Проверка в базе данных
            // return await _context.Users.AnyAsync(u => u.Email == email);
            return false;
        }

        /*
        private string HashPassword(string password)
        {
            // Использовать BCrypt, PBKDF2 или другой безопасный алгоритм
            // Пример с BCrypt (нужен пакет BCrypt.Net-Next)
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
        */

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
