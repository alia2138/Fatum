using TaroPlatform.Application.Common.Results;
using TaroPlatform.Domain.Enums;
using TaroPlatform.Domain.Users;

namespace TaroPlatform.Application.Auth;

/// <summary>
/// Фабрика (Factory Method): створює потрібний тип користувача залежно від ролі.
/// Перевіряє поля, специфічні для кожної ролі.
/// </summary>
public static class UserFactory
{
    public static Result<User> Create(RegisterRequest request, string passwordHash) =>
        request.Role switch
        {
            UserRole.Client => CreateClient(request, passwordHash),
            UserRole.Consultant => Result.Success<User>(new Consultant(request.Email, passwordHash)),
            _ => Result.Failure<User>("Недопустима роль. Оберіть клієнта або таролога.")
        };

    private static Result<User> CreateClient(RegisterRequest request, string passwordHash)
    {
        if (request.BirthDate is null)
            return Result.Failure<User>("Для клієнта потрібно вказати дату народження.");

        if (request.BirthDate.Value.Date > DateTime.UtcNow.Date)
            return Result.Failure<User>("Дата народження не може бути в майбутньому.");

        return Result.Success<User>(new Client(request.Email, passwordHash, request.BirthDate.Value));
    }
}
