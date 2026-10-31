using TaroPlatform.Infrastructure.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Tarot;
using TaroPlatform.Infrastructure.AI;
using TaroPlatform.Infrastructure.Persistence;
using TaroPlatform.Infrastructure.Security;
using TaroPlatform.Infrastructure.Services;
using TaroPlatform.Infrastructure.Tarot;

namespace TaroPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        MongoMappings.Register();

        services.Configure<MongoSettings>(configuration.GetSection(MongoSettings.SectionName));
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<TelegramSettings>(configuration.GetSection(TelegramSettings.SectionName));
        services.Configure<AiSettings>(configuration.GetSection(AiSettings.SectionName));
        services.Configure<NotificationSettings>(configuration.GetSection(NotificationSettings.SectionName));

        services.AddSingleton<MongoContext>();
        services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITarotReadingRepository, TarotReadingRepository>();
services.AddScoped<IConsultationRepository, ConsultationRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();
        services.AddScoped<ISavedMatrixRepository, SavedMatrixRepository>();
        services.AddHttpClient<TelegramBotNotifier>();
        services.AddSingleton<EmailNotifier>();
        services.AddScoped<IExternalNotifier, ExternalNotifier>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<ITelegramVerifier, TelegramVerifier>();
        services.AddSingleton<IRandomSource, SystemRandomSource>();

        // AI: справжня модель + заглушка як запасний варіант
        services.AddSingleton<StubTarotInterpreter>();
        services.AddHttpClient<OpenAiTarotInterpreter>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<AiSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        });
        services.AddScoped<ITarotInterpreter, FallbackTarotInterpreter>();

        return services;
    }
}
