using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Olimpiadnic.Data;
using Olimpiadnic.Models.RoleModels;
using Olimpiadnic.Services;
using Olimpiadnic.Services.Repos;

var builder = WebApplication.CreateBuilder(args);

// Регистрируем наш контекст в контейнере зависимостей
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.
builder.Services.AddControllersWithViews();

// 1. Добавляем сервисы для работы с сессией
builder.Services.AddDistributedMemoryCache(); // Хранилище сессий в памяти
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = ".Olympiad.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
builder.Services.AddHttpContextAccessor();

// Добавление кастомных сервисов
builder.Services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
builder.Services.AddScoped<IInviteService, InviteService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();

builder.Services.AddScoped<IOlympiadRepository, OlympiadRepository>();
builder.Services.AddScoped<IOlympiadSessionService, OlympiadSessionService>();
builder.Services.AddScoped<IOlympiadEditorSessionService, OlympiadEditorSessionService>();

// Настройка аутентификации через cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
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
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapStaticAssets();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession(); 

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "olympiad_details",
    pattern: "Olympiad/Details/{id}",
    defaults: new { controller = "Olympiad", action = "Details" });

app.MapControllers();

app.Run();
