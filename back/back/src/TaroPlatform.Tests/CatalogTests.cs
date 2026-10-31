using TaroPlatform.Application.Catalog;
using TaroPlatform.Domain.Schedule;
using TaroPlatform.Domain.Tarot;
using TaroPlatform.Domain.Users;
using Xunit;

namespace TaroPlatform.Tests;

public class CatalogTests
{
    [Fact]
    public void SlotStarts_FillIntervalWithSessions()
    {
        var intervals = new[] { new WorkingInterval(DayOfWeek.Monday, 600, 720) };

        var starts = WorkingInterval.GenerateSlotStarts(intervals, DayOfWeek.Monday, 60);

        Assert.Equal(new[] { 600, 660 }, starts);
    }

    [Fact]
    public async Task Schedule_RejectsOverlappingIntervals()
    {
        var repo = new FakeUserRepository();
        var consultant = new Consultant("tarot@test.com", "hash");
        await repo.AddAsync(consultant);
        var service = new ConsultantService(repo);

        var request = new UpdateScheduleRequest("Europe/Kyiv", new List<ScheduleIntervalDto>
        {
            new("Monday", "10:00", "12:00"),
            new("Monday", "11:00", "13:00")
        });

        var result = await service.UpdateScheduleAsync(consultant.Id, request);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Slots_ReturnFreeSlotsForWorkingDay()
    {
        var repo = new FakeUserRepository();
        var consultant = new Consultant("tarot@test.com", "hash");
        consultant.SetAcceptingClients(true);
        consultant.SetSessionMinutes(60);
        consultant.SetSchedule(new[] { new WorkingInterval(DayOfWeek.Monday, 600, 720) });
        await repo.AddAsync(consultant);
        var service = new ConsultantService(repo);

        var result = await service.GetSlotsAsync(consultant.Id, new DateOnly(2026, 11, 2));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Slots.Count);
    }
}
