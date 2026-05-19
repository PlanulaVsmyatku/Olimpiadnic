using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Olimpiadnic.Data;
using Olimpiadnic.Models.RoleModels;
using Olimpiadnic.Services;

var builder = WebApplication.CreateBuilder(args);

// Регистрируем наш контекст в контейнере зависимостей
builder.Services.AddDbContext<AppDbContext>(options =>
    // Указываем, что используем SQL Server (SSMS) и берем строку из настроек
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.
builder.Services.AddControllersWithViews();

// Добавление сессий (для временного хранения ответов)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Добавление кастомных сервисов
// Добавление сервиса восстановления паролей
builder.Services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
// Добавление сервиса инвайт-ссылок
builder.Services.AddScoped<IInviteService, InviteService>();
// Добавление сервиса для работы с паролями
builder.Services.AddScoped<IPasswordService, PasswordService>();

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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StaffOnly", policy =>
        policy.RequireRole(UserRoles.staff, UserRoles.admin));
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole(UserRoles.admin));
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
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
