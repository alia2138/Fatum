using TaroPlatform.Application.Auth;
using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Domain.Enums;
using TaroPlatform.Domain.Users;
using Xunit;

namespace TaroPlatform.Tests;

public class AuthServiceTests
{
    private static AuthService CreateService() =>
        new(new FakeUserRepository(), new FakeHasher(), new FakeJwt(), new FakeTelegram());

    [Fact]
    public async Task Register_Consultant_WithoutBirthDate_Succeeds()
    {
        var service = CreateService();
        var request = new RegisterRequest(UserRole.Consultant, "tarot@test.com", "123456", "123456", null);

        var result = await service.RegisterAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal("Consultant", result.Value.Role);
    }

    [Fact]
    public async Task Register_Client_WithoutBirthDate_Fails()
    {
        var service = CreateService();
        var request = new RegisterRequest(UserRole.Client, "client@test.com", "123456", "123456", null);

        var result = await service.RegisterAsync(request);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Register_MismatchedPasswords_Fails()
    {
        var service = CreateService();
        var request = new RegisterRequest(UserRole.Consultant, "tarot@test.com", "123456", "654321", null);

        var result = await service.RegisterAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Паролі не співпадають.", result.Error);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Fails()
    {
        var service = CreateService();
        var request = new RegisterRequest(UserRole.Consultant, "tarot@test.com", "123456", "123456", null);

        await service.RegisterAsync(request);
        var second = await service.RegisterAsync(request);

        Assert.True(second.IsFailure);
    }

    [Fact]
    public async Task Login_WrongPassword_Fails()
    {
        var service = CreateService();
        await service.RegisterAsync(new RegisterRequest(UserRole.Consultant, "tarot@test.com", "123456", "123456", null));

        var result = await service.LoginAsync(new LoginRequest("tarot@test.com", "wrong"));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ExternalLogin_NewClient_CreatesUserOnce()
    {
        var service = CreateService();
        var identity = new ExternalIdentity("google", "g-123", "social@test.com", UserRole.Client, new DateTime(1995, 5, 20));

        var first = await service.ExternalLoginAsync(identity);
        var second = await service.ExternalLoginAsync(identity);

        Assert.True(first.IsSuccess);
        Assert.Equal("Client", first.Value.Role);
        Assert.Equal(first.Value.UserId, second.Value.UserId);
    }

    [Fact]
    public async Task ExternalLogin_NewClient_WithoutBirthDate_Fails()
    {
        var service = CreateService();
        var identity = new ExternalIdentity("github", "gh-1", "dev@test.com", UserRole.Client, null);

        var result = await service.ExternalLoginAsync(identity);

        Assert.True(result.IsFailure);
    }
}

internal class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Email == email));

    public Task<User?> GetByExternalLoginAsync(string provider, string providerUserId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.FirstOrDefault(u => u.HasExternalLogin(provider, providerUserId)));

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<User>>(_users.ToList());

    public Task AddAsync(User entity, CancellationToken cancellationToken = default)
    {
        _users.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _users.RemoveAll(u => u.Id == id);
        return Task.CompletedTask;
    }
}

internal class FakeHasher : IPasswordHasher
{
    public string Hash(string password) => "hash:" + password;
    public bool Verify(string password, string hash) => hash == "hash:" + password;
}

internal class FakeJwt : IJwtTokenGenerator
{
    public string Generate(User user) => "token-" + user.Id;
}

internal class FakeTelegram : ITelegramVerifier
{
    public bool Verify(IReadOnlyDictionary<string, string> fields, string hash, long authDate) => hash == "valid";
}
