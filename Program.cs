using Microsoft.AspNetCore.Authentication.Cookies;
using Olimpiadnic.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Добавление кастомных сервисов
builder.Services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();

// Настройка аутентификации через cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";        // Путь к странице входа
        options.LogoutPath = "/Account/Logout";      // Путь к выходу
        options.AccessDeniedPath = "/Home/AccessDenied"; // Страница отказа в доступе
        options.ExpireTimeSpan = TimeSpan.FromDays(7);    // Время жизни cookie
        options.SlidingExpiration = true;                  // Обновление при активности
        options.Cookie.HttpOnly = true;                    // Защита от XSS
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Только HTTPS
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.MapStaticAssets();

app.UseRouting();

app.UseAuthentication();  // Кто это?
app.UseAuthorization();   // Может ли он это делать?


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
