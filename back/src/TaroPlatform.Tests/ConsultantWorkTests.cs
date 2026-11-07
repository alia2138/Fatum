using TaroPlatform.Application.Catalog;
using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Common.Results;
using TaroPlatform.Domain.Consultations;
using TaroPlatform.Domain.Schedule;
using TaroPlatform.Domain.Users;
using Xunit;

namespace TaroPlatform.Tests;

public class ConsultantWorkTests
{
    [Fact]
    public async Task ScheduleChange_KeepsPreviousScheduleInHistory()
    {
        var users = new FakeUserRepository();
        var consultant = new Consultant("work@test.com", "hash");
        await users.AddAsync(consultant);
        var service = new ConsultantService(users);
        var work = new ConsultantWorkService(users, new ConsultationStore());

        await service.UpdateScheduleAsync(consultant.Id, new UpdateScheduleRequest("Europe/Kyiv",
            new List<ScheduleIntervalDto> { new("Monday", "10:00", "12:00") }));
        await service.UpdateScheduleAsync(consultant.Id, new UpdateScheduleRequest("Europe/Kyiv",
            new List<ScheduleIntervalDto> { new("Tuesday", "10:00", "12:00") }));

        var history = await work.GetScheduleHistoryAsync(consultant.Id);

        // Поточний графік + один попередній
        Assert.Equal(2, history.Value.Count);
        Assert.Null(history.Value[0].ValidTo);
        Assert.NotNull(history.Value[1].ValidTo);
        Assert.Equal("Monday", history.Value[1].Intervals[0].Day);
    }

    [Fact]
    public async Task WorkSummary_CountsAvailableAndCompletedHours()
    {
        var users = new FakeUserRepository();
        var consultations = new ConsultationStore();
        var consultant = new Consultant("work2@test.com", "hash");
        consultant.SetSessionMinutes(60);
        consultant.SetSchedule(new[] { new WorkingInterval(DayOfWeek.Monday, 600, 720) });
        await users.AddAsync(consultant);

        var consultation = new Consultation(
            "client-1", consultant.Id, new DateTime(2026, 11, 2, 8, 0, 0, DateTimeKind.Utc), 60, null, "Питання для тесту");
        consultation.Confirm(null, DateTime.UtcNow);
        consultation.Complete(DateTime.UtcNow);
        await consultations.AddAsync(consultation);

        var work = new ConsultantWorkService(users, consultations);
        var summary = await work.GetWorkSummaryAsync(consultant.Id, new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 8));

        Assert.True(summary.IsSuccess);
        Assert.Equal(2.0, summary.Value.AvailableHours);
        Assert.Equal(1.0, summary.Value.CompletedHours);
        Assert.Equal(1, summary.Value.CompletedCount);
    }

    private sealed class ConsultationStore : IConsultationRepository
    {
        private readonly List<Consultation> _items = new();

        public Task<Consultation?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c => c.Id == id));

        public Task<IReadOnlyList<Consultation>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Consultation>>(_items.ToList());

        public Task AddAsync(Consultation entity, CancellationToken cancellationToken = default)
        {
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Consultation entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            _items.RemoveAll(c => c.Id == id);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Consultation>> GetByParticipantAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Consultation>>(_items.Where(c => c.ClientId == userId || c.ConsultantId == userId).ToList());

        public Task<bool> HasActiveAtAsync(string consultantId, DateTime startUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<Consultation>> GetDueRemindersAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Consultation>>(new List<Consultation>());
    }
}
