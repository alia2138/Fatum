using TaroPlatform.Application.Chat;
using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Consultations;
using TaroPlatform.Application.Notifications;
using TaroPlatform.Domain.Chat;
using TaroPlatform.Domain.Consultations;
using TaroPlatform.Domain.Notifications;
using TaroPlatform.Domain.Schedule;
using TaroPlatform.Domain.Tarot;
using TaroPlatform.Domain.Users;
using Xunit;

namespace TaroPlatform.Tests;

public class BookingTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc);

    // Понеділок 2 листопада 2026, 10:00 за Києвом (UTC+2)
    private static readonly DateTimeOffset SlotUtc = new(2026, 11, 2, 8, 0, 0, TimeSpan.Zero);

    private const string Question = "Чи варто змінювати роботу найближчим часом?";

    [Fact]
    public async Task Book_ValidSlot_CreatesPending_AndNotifiesConsultant()
    {
        var env = new Env();

        var result = await env.Service.BookAsync(env.Client.Id,
            new BookConsultationRequest(env.Consultant.Id, SlotUtc, ReadingTopic.Career, Question));

        Assert.True(result.IsSuccess);
        Assert.Equal("Pending", result.Value.Status);
        Assert.Single(await env.NotificationRepo.GetByUserAsync(env.Consultant.Id));
    }

    [Fact]
    public async Task Book_TimeOutsideSchedule_Fails()
    {
        var env = new Env();

        // 13:00 за Києвом — поза графіком (10:00–12:00)
        var outside = new DateTimeOffset(2026, 11, 2, 11, 0, 0, TimeSpan.Zero);
        var result = await env.Service.BookAsync(env.Client.Id,
            new BookConsultationRequest(env.Consultant.Id, outside, null, Question));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Book_SameSlotTwice_SecondFails()
    {
        var env = new Env();
        var request = new BookConsultationRequest(env.Consultant.Id, SlotUtc, null, Question);

        await env.Service.BookAsync(env.Client.Id, request);
        var second = await env.Service.BookAsync(env.Client.Id, request);

        Assert.True(second.IsFailure);
    }

    [Fact]
    public async Task Confirm_OpensChat_WithLink_AndNotifiesClient()
    {
        var env = new Env();
        var booked = await env.Service.BookAsync(env.Client.Id,
            new BookConsultationRequest(env.Consultant.Id, SlotUtc, null, Question));

        var confirmed = await env.Service.ConfirmAsync(env.Consultant.Id, booked.Value.Id,
            new ConfirmConsultationRequest(null));

        Assert.True(confirmed.IsSuccess);
        Assert.Equal("Confirmed", confirmed.Value.Status);
        Assert.NotNull(confirmed.Value.ConversationId);

        var messages = await env.Chat.GetMessagesAsync(env.Client.Id, confirmed.Value.ConversationId!);
        Assert.True(messages.IsSuccess);
        Assert.Contains(messages.Value, m => m.Kind == "ConsultationLink" && m.Link == confirmed.Value.Link);

        Assert.Single(await env.NotificationRepo.GetByUserAsync(env.Client.Id));
    }

    [Fact]
    public async Task Confirm_ByClient_Fails()
    {
        var env = new Env();
        var booked = await env.Service.BookAsync(env.Client.Id,
            new BookConsultationRequest(env.Consultant.Id, SlotUtc, null, Question));

        var result = await env.Service.ConfirmAsync(env.Client.Id, booked.Value.Id,
            new ConfirmConsultationRequest(null));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Client_CanCancel_ConfirmedConsultation()
    {
        var env = new Env();
        var booked = await env.Service.BookAsync(env.Client.Id,
            new BookConsultationRequest(env.Consultant.Id, SlotUtc, null, Question));
        await env.Service.ConfirmAsync(env.Consultant.Id, booked.Value.Id, new ConfirmConsultationRequest(null));

        var cancelled = await env.Service.CancelAsync(env.Client.Id, booked.Value.Id);

        Assert.True(cancelled.IsSuccess);
        Assert.Equal("CancelledByClient", cancelled.Value.Status);
    }

    /// <summary>Спільне середовище тесту: користувачі, репозиторії і сервіси.</summary>
    private sealed class Env
    {
        public readonly FakeUserRepository Users = new();
        public readonly FakeConsultationRepository Consultations = new();
        public readonly FakeConversationRepository Conversations = new();
        public readonly FakeNotificationRepository NotificationRepo = new();
        public readonly ChatService Chat;
        public readonly NotificationService Notifications;
        public readonly ConsultationService Service;
        public readonly Consultant Consultant;
        public readonly Client Client;

        public Env()
        {
            var clock = new FixedClock(Now);
            Chat = new ChatService(Conversations, Users);
            Notifications = new NotificationService(NotificationRepo, Users, new NoopExternal());
            Service = new ConsultationService(Consultations, Users, Chat, Notifications, clock);

            Consultant = new Consultant("tarot@test.com", "hash");
            Consultant.SetDisplayName("Олена");
            Consultant.SetAcceptingClients(true);
            Consultant.SetSessionMinutes(60);
            Consultant.SetTimezone("Europe/Kyiv");
            Consultant.SetSchedule(new[] { new WorkingInterval(DayOfWeek.Monday, 600, 720) });

            Client = new Client("client@test.com", "hash", new DateTime(1995, 5, 20));
            Client.SetDisplayName("Марія");

            Users.AddAsync(Consultant).GetAwaiter().GetResult();
            Users.AddAsync(Client).GetAwaiter().GetResult();
        }
    }

    private sealed class NoopExternal : IExternalNotifier
    {
        public Task SendAsync(TaroPlatform.Domain.Users.User user, string title, string body, string? link, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public FixedClock(DateTime now) => UtcNow = now;
        public DateTime UtcNow { get; }
    }

    private sealed class FakeConsultationRepository : IConsultationRepository
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
            Task.FromResult<IReadOnlyList<Consultation>>(
                _items.Where(c => c.ClientId == userId || c.ConsultantId == userId).ToList());

        public Task<IReadOnlyList<Consultation>> GetDueRemindersAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Consultation>>(_items.Where(c => c.Status == ConsultationStatus.Confirmed && c.StartUtc >= fromUtc && c.StartUtc <= toUtc && !c.ReminderSent).ToList());

        public Task<bool> HasActiveAtAsync(string consultantId, DateTime startUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Any(c => c.ConsultantId == consultantId && c.StartUtc == startUtc && c.IsActive()));
    }

    private sealed class FakeConversationRepository : IConversationRepository
    {
        private readonly List<Conversation> _items = new();

        public Task<Conversation?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c => c.Id == id));

        public Task<IReadOnlyList<Conversation>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Conversation>>(_items.ToList());

        public Task AddAsync(Conversation entity, CancellationToken cancellationToken = default)
        {
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Conversation entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            _items.RemoveAll(c => c.Id == id);
            return Task.CompletedTask;
        }

        public Task<Conversation?> GetByPairAsync(string clientId, string consultantId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c => c.ClientId == clientId && c.ConsultantId == consultantId));

        public Task<IReadOnlyList<Conversation>> GetByParticipantAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Conversation>>(
                _items.Where(c => c.ClientId == userId || c.ConsultantId == userId).ToList());
    }

    private sealed class FakeNotificationRepository : INotificationRepository
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
}
