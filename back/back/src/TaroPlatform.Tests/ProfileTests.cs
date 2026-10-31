using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Profile;
using TaroPlatform.Domain.Tarot;
using TaroPlatform.Domain.Users;
using Xunit;

namespace TaroPlatform.Tests;

public class ProfileTests
{
    [Fact]
    public async Task NewConsultant_IsIncomplete_AndListsMissingFields()
    {
        var repo = new FakeUserRepository();
        var consultant = new Consultant("tarot@test.com", "hash");
        await repo.AddAsync(consultant);
        var service = new ProfileService(repo, new FakePhotoStorage());

        var result = await service.GetMeAsync(consultant.Id);

        Assert.False(result.Value.IsComplete);
        Assert.Contains("Фото", result.Value.MissingFields);
        Assert.Contains("Ім'я", result.Value.MissingFields);
    }

    [Fact]
    public async Task Consultant_CanCompleteProfile_AndAcceptClients()
    {
        var repo = new FakeUserRepository();
        var consultant = new Consultant("tarot@test.com", "hash");
        await repo.AddAsync(consultant);
        var service = new ProfileService(repo, new FakePhotoStorage());

        await service.UpdateConsultantAsync(consultant.Id, new ConsultantProfileUpdate(
            "Олена", "Працюю з таро понад 5 років.", new List<ReadingTopic> { ReadingTopic.Love },
            5, new List<string> { "Українська" }, 500, 60, null, null));
        await service.UploadPhotoAsync(consultant.Id, Stream.Null, "image/png", 1000);

        var accept = await service.UpdateConsultantAsync(consultant.Id, new ConsultantProfileUpdate(
            null, null, null, null, null, null, null, true, null));

        Assert.True(accept.IsSuccess);
        Assert.True(accept.Value.IsComplete);
        Assert.True(accept.Value.Consultant!.IsAcceptingClients);
    }

    [Fact]
    public async Task Consultant_CannotAcceptClients_WhenProfileIncomplete()
    {
        var repo = new FakeUserRepository();
        var consultant = new Consultant("tarot@test.com", "hash");
        await repo.AddAsync(consultant);
        var service = new ProfileService(repo, new FakePhotoStorage());

        var result = await service.UpdateConsultantAsync(consultant.Id, new ConsultantProfileUpdate(
            null, null, null, null, null, null, null, true, null));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Client_CanUpdateNameAndInterests_AndBecomesComplete()
    {
        var repo = new FakeUserRepository();
        var client = new Client("client@test.com", "hash", new DateTime(1995, 5, 20));
        await repo.AddAsync(client);
        var service = new ProfileService(repo, new FakePhotoStorage());

        var result = await service.UpdateClientAsync(client.Id, new ClientProfileUpdate(
            "Марія", null, null, "Europe/Kyiv", new List<ReadingTopic> { ReadingTopic.Career }));

        Assert.True(result.IsSuccess);
        Assert.Equal("Марія", result.Value.DisplayName);
        Assert.True(result.Value.IsComplete);
        Assert.Contains("Career", result.Value.Client!.Interests);
    }

    [Fact]
    public async Task Client_CannotUseConsultantUpdate()
    {
        var repo = new FakeUserRepository();
        var client = new Client("client@test.com", "hash", new DateTime(1995, 5, 20));
        await repo.AddAsync(client);
        var service = new ProfileService(repo, new FakePhotoStorage());

        var result = await service.UpdateConsultantAsync(client.Id, new ConsultantProfileUpdate(
            "Марія", null, null, null, null, null, null, null, null));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Photo_RejectsUnsupportedType()
    {
        var repo = new FakeUserRepository();
        var client = new Client("client@test.com", "hash", new DateTime(1995, 5, 20));
        await repo.AddAsync(client);
        var service = new ProfileService(repo, new FakePhotoStorage());

        var result = await service.UploadPhotoAsync(client.Id, Stream.Null, "application/pdf", 1000);

        Assert.True(result.IsFailure);
    }
}

internal class FakePhotoStorage : IPhotoStorage
{
    public Task<string> SaveAsync(string userId, Stream content, string extension, CancellationToken cancellationToken = default) =>
        Task.FromResult($"/uploads/avatars/{userId}{extension}");

    public Task DeleteAsync(string url, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
