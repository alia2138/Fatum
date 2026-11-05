using TaroPlatform.Application.Admin;
using TaroPlatform.Application.Auth;
using TaroPlatform.Domain.Users;
using Xunit;

namespace TaroPlatform.Tests;

public class AdminTests
{
    [Fact]
    public async Task BlockedUser_CannotLogin()
    {
        var users = new FakeUserRepository();
        var auth = new AuthService(users, new FakeHasher(), new FakeJwt(), new FakeTelegram());
        await auth.RegisterAsync(new RegisterRequest(TaroPlatform.Domain.Enums.UserRole.Consultant, "blocked@test.com", "123456", "123456", null));

        var user = await users.GetByEmailAsync("blocked@test.com");
        user!.Block();

        var result = await auth.LoginAsync(new LoginRequest("blocked@test.com", "123456"));

        Assert.True(result.IsFailure);
        Assert.Contains("заблоковано", result.Error);
    }

    [Fact]
    public async Task Bootstrap_CreatesAdminOnlyOnce()
    {
        var users = new FakeUserRepository();
        var bootstrap = new AdminBootstrap(users, new FakeHasher());

        await bootstrap.EnsureAsync("Admin@Test.com", "123456");
        await bootstrap.EnsureAsync("admin@test.com", "123456");

        var all = await users.GetAllAsync();
        Assert.Single(all);
        Assert.IsType<Administrator>(all[0]);
    }
}
