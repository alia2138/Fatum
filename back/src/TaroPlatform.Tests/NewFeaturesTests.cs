using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Common.Results;
using TaroPlatform.Application.Consultations;
using TaroPlatform.Application.Matrix;
using TaroPlatform.Application.Notifications;
using TaroPlatform.Application.Reminders;
using TaroPlatform.Application.Tarot;
using TaroPlatform.Domain.Consultations;
using TaroPlatform.Domain.Matrix;
using TaroPlatform.Domain.Notifications;
using TaroPlatform.Domain.Schedule;
using TaroPlatform.Domain.Users;
using Xunit;

namespace TaroPlatform.Tests;

public class NewFeaturesTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Zones_UseOnlyPointsOfVideoMatrix()
    {
        var matrix = DestinyMatrixCalculator.Calculate(new DateOnly(1968, 8, 17), ChannelMethod.Video);

        foreach (var zone in MatrixZones.All)
            foreach (var key in zone.PointKeys)
                Assert.True(matrix.Points.ContainsKey(key), $"Зона {zone.Title}: немає точки {key}");
    }

    [Fact]
    public async Task Reminder_SendsOnce_ToClientAndConsultant()
    {
        var users = new FakeUserRepository();
        var consultations = new ReminderConsultations();
        var notificationRepo = new ReminderNotifications();
        var clock = new FixedClock(Now);
        var notifications = new NotificationService(notificationRepo, users, new NoopExternal());
        var reminders = new ReminderService(consultations, users, notifications, clock);

        var consultant = new Consultant("tarot@test.com", "hash");
        consultant.SetTimezone("Europe/Kyiv");
        var client = new Client("client@test.com", "hash", new DateTime(1995, 5, 20));
        await users.AddAsync(consultant);
        await users.AddAsync(client);

        // Консультація через 30 хвилин — потрапляє в вікно нагадування
        var consultation = new Consultation(client.Id, consultant.Id, Now.AddMinutes(30), 60, null, "Питання для тесту");
        consultation.Confirm(null, Now);
        await consultations.AddAsync(consultation);

        var first = await reminders.SendDueAsync();
        var second = await reminders.SendDueAsync();

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        Assert.Single(await notificationRepo.GetByUserAsync(client.Id));
        Assert.Single(await notificationRepo.GetByUserAsync(consultant.Id));
    }

    [Fact]
    public async Task SavedMatrix_Question_KeepsConversationAndZoneFocus()
    {
        var saved = new SavedMatrixStore();
        var service = new SavedMatrixService(saved, new FakeInterpreter(), new FixedClock(Now));

        var created = await service.SaveAsync("user-1", new SaveMatrixRequest(new DateOnly(1968, 8, 17), null, "Кузьма"));
        var first = await service.AskAsync("user-1", created.Value.Id, new MatrixQuestionRequest("Які мої грошові таланти?", "finance"));
        var second = await service.AskAsync("user-1", created.Value.Id, new MatrixQuestionRequest("А що з стосунками?", null));

        Assert.True(first.IsSuccess);
        Assert.Equal("finance", first.Value.Messages[0].Zone);
        Assert.Equal(4, second.Value.Messages.Count);

        var stranger = await service.AskAsync("user-2", created.Value.Id, new MatrixQuestionRequest("Це я?", null));
        Assert.True(stranger.IsFailure);
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public FixedClock(DateTime now) => UtcNow = now;
        public DateTime UtcNow { get; }
    }

    private sealed class NoopExternal : IExternalNotifier
    {
        public Task SendAsync(User user, string title, string body, string? link, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeInterpreter : ITarotInterpreter
    {
        public Task<InterpretationResult> InterpretAsync(InterpretationContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(new InterpretationResult("тестова відповідь", "fake"));
    }

    private sealed class ReminderConsultations : IConsultationRepository
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
            Task.FromResult<IReadOnlyList<Consultation>>(_items
                .Where(c => c.Status == ConsultationStatus.Confirmed && c.StartUtc >= fromUtc && c.StartUtc <= toUtc && !c.ReminderSent)
                .ToList());
    }

    private sealed class ReminderNotifications : INotificationRepository
    {
        private readonly List<Notification> _items = new();

        public Task<Notification?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(n => n.Id == id));

        public Task<IReadOnlyList<Notification>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Notification>>(_items.ToList());

        public Task AddAsync(Notification entity, CancellationToken cancellationToken = default)
        {
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Notification entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            _items.RemoveAll(n => n.Id == id);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Notification>> GetByUserAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Notification>>(_items.Where(n => n.UserId == userId).ToList());
    }

    private sealed class SavedMatrixStore : ISavedMatrixRepository
    {
        private readonly List<SavedMatrix> _items = new();

        public Task<SavedMatrix?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(m => m.Id == id));

        public Task<IReadOnlyList<SavedMatrix>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SavedMatrix>>(_items.ToList());

        public Task AddAsync(SavedMatrix entity, CancellationToken cancellationToken = default)
        {
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(SavedMatrix entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            _items.RemoveAll(m => m.Id == id);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SavedMatrix>> GetByUserAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SavedMatrix>>(_items.Where(m => m.UserId == userId).ToList());

        public Task<SavedMatrix?> GetOwnedAsync(string id, string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(m => m.Id == id && m.UserId == userId));
    }
}
