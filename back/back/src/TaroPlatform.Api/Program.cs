using TaroPlatform.Application.Favorites;
using TaroPlatform.Application.Reminders;
using TaroPlatform.Api.Workers;
using TaroPlatform.Application.Matrix;
using TaroPlatform.Application.Reviews;
using TaroPlatform.Application.Chat;
using TaroPlatform.Application.Consultations;
using TaroPlatform.Application.Notifications;
using TaroPlatform.Application.Catalog;
using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Profile;
using TaroPlatform.Infrastructure.Storage;
using TaroPlatform.Application.Tarot;
using System.Text;
using System.Text.Json.Serialization;
using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TaroPlatform.Api.Middleware;
using TaroPlatform.Application.Auth;
using TaroPlatform.Infrastructure;
using TaroPlatform.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Локальні секрети (ключі соцмереж). Файл у .gitignore, у репозиторій не потрапляє.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Вставте токен, отриманий з /api/auth/login (без слова Bearer)"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<CardDrawService>();
builder.Services.AddScoped<ITarotService, TarotService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IConsultantService, ConsultantService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IConsultationService, ConsultationService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IMatrixService, MatrixService>();
builder.Services.AddScoped<ISavedMatrixService, SavedMatrixService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IReminderService, ReminderService>();
builder.Services.AddHostedService<ReminderWorker>();
builder.Services.AddSingleton<IPhotoStorage>(new LocalPhotoStorage(
    Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads", "avatars")));

var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Не задано секцію Jwt у appsettings.json");

var auth = builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
    });

// Тимчасове cookie, у якому зберігається результат входу через Google / GitHub до завершення
auth.AddCookie("External", o =>
{
    o.Cookie.Name = "TaroExternal";
    o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
});

// Провайдер вмикається лише якщо його ключі задані в appsettings.Local.json
var googleId = builder.Configuration["Authentication:Google:ClientId"];
if (!string.IsNullOrWhiteSpace(googleId))
{
    auth.AddGoogle(o =>
    {
        o.ClientId = googleId;
        o.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
        o.SignInScheme = "External";
    });
}

var githubId = builder.Configuration["Authentication:GitHub:ClientId"];
if (!string.IsNullOrWhiteSpace(githubId))
{
    auth.AddGitHub(o =>
    {
        o.ClientId = githubId;
        o.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"] ?? string.Empty;
        o.Scope.Add("user:email");
        o.SignInScheme = "External";
    });
}

builder.Services.AddAuthorization();

// CORS: дозволяємо фронту (окремий origin) викликати API
var allowedOrigins = builder.Configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:63342" };
builder.Services.AddCors(o => o.AddPolicy("Frontend", p =>
    p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();   // тестова сторінка для Telegram: wwwroot/telegram-login.html
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
