using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Common.Results;
using TaroPlatform.Application.Consultations;
using TaroPlatform.Application.Notifications;
using TaroPlatform.Application.Reviews;
using TaroPlatform.Domain.Consultations;
using TaroPlatform.Domain.Reviews;
using TaroPlatform.Domain.Users;
using Xunit;

namespace TaroPlatform.Tests;

public class ReviewTests
{
    [Fact]
    public async Task Review_AfterCompletedConsultation_UpdatesConsultantRating()
    {
        var env = new Env();
        var consultation = await env.CompletedConsultationAsync();

        var result = await env.Service.CreateAsync(env.Client.Id, new ReviewCreateRequest(consultation.Id, 4, "Дуже добре"));

        Assert.True(result.IsSuccess);
        Assert.Equal(4.0, env.Consultant.RatingAverage);
        Assert.Equal(1, env.Consultant.ReviewsCount);
    }

    [Fact]
    public async Task Rating_IsAverageOfAllReviews()
    {
        var env = new Env();
        var first = await env.CompletedConsultationAsync();
        var second = await env.CompletedConsultationAsync();

        await env.Service.CreateAsync(env.Client.Id, new ReviewCreateRequest(first.Id, 4, null));
        await env.Service.CreateAsync(env.Client.Id, new ReviewCreateRequest(second.Id, 2, null));

        Assert.Equal(3.0, env.Consultant.RatingAverage);
        Assert.Equal(2, env.Consultant.ReviewsCount);
    }

    [Fact]
    public async Task Review_WithoutCompletedConsultation_Fails()
    {
        var env = new Env();
        var pending = new Consultation(env.Client.Id, env.Consultant.Id, new DateTime(2026, 11, 2, 8, 0, 0, DateTimeKind.Utc), 60, null, "Питання для тесту");
        await env.Consultations.AddAsync(pending);

        var result = await env.Service.CreateAsync(env.Client.Id, new ReviewCreateRequest(pending.Id, 5, null));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Review_Duplicate_OnSameConsultation_Fails()
    {
        var env = new Env();
        var consultation = await env.CompletedConsultationAsync();
        await env.Service.CreateAsync(env.Client.Id, new ReviewCreateRequest(consultation.Id, 5, null));

        var second = await env.Service.CreateAsync(env.Client.Id, new ReviewCreateRequest(consultation.Id, 3, null));

        Assert.True(second.IsFailure);
    }

    [Fact]
    public async Task Review_RatingOutOfRange_Fails()
    {
        var env = new Env();
        var consultation = await env.CompletedConsultationAsync();

        var result = await env.Service.CreateAsync(env.Client.Id, new ReviewCreateRequest(consultation.Id, 6, null));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Update_ChangesRating_AndRecalculates()
    {
        var env = new Env();
        var consultation = await env.CompletedConsultationAsync();
        var created = await env.Service.CreateAsync(env.Client.Id, new ReviewCreateRequest(consultation.Id, 5, null));

        var updated = await env.Service.UpdateAsync(env.Client.Id, created.Value.Id, new ReviewUpdateRequest(3, null));

        Assert.True(updated.IsSuccess);
        Assert.Equal(3.0, env.Consultant.RatingAverage);
    }

    [Fact]
    public async Task Reply_OnlyByReviewedConsultant_AndOnlyOnce()
    {
        var env = new Env();
        var consultation = await env.CompletedConsultationAsync();
        var created = await env.Service.CreateAsync(env.Client.Id, new ReviewCreateRequest(consultation.Id, 5, "Дякую"));

        var stranger = await env.Service.ReplyAsync(env.OtherConsultant.Id, created.Value.Id, new ReviewReplyRequest("Відповідь"));
        var owner = await env.Service.ReplyAsync(env.Consultant.Id, created.Value.Id, new ReviewReplyRequest("Дякую вам!"));
        var again = await env.Service.ReplyAsync(env.Consultant.Id, created.Value.Id, new ReviewReplyRequest("Ще раз"));

        Assert.True(stranger.IsFailure);
        Assert.True(owner.IsSuccess);
        Assert.True(again.IsFailure);
    }

    /// <summary>Спільне середовище: таролог, клієнт, другий таролог і сервіс відгуків.</summary>
    private sealed class Env
    {
        public readonly FakeUserRepository Users = new();
        public readonly ReviewStore Reviews = new();
        public readonly ConsultationStore Consultations = new();
        public readonly ReviewService Service;
        public readonly Consultant Consultant;
        public readonly Consultant OtherConsultant;
        public readonly Client Client;

        public Env()
        {
            Service = new ReviewService(Reviews, Consultations, Users, new NoopNotifications(), new FixedClock(new DateTime(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc)));

            Consultant = new Consultant("tarot@test.com", "hash");
            Consultant.SetDisplayName("Олена");
            OtherConsultant = new Consultant("other@test.com", "hash");
            OtherConsultant.SetDisplayName("Інший таролог");
            Client = new Client("client@test.com", "hash", new DateTime(1995, 5, 20));
            Client.SetDisplayName("Марія");

            Users.AddAsync(Consultant).GetAwaiter().GetResult();
            Users.AddAsync(OtherConsultant).GetAwaiter().GetResult();
            Users.AddAsync(Client).GetAwaiter().GetResult();
        }

        /// <summary>Проведена консультація клієнта з таологом.</summary>
        public async Task<Consultation> CompletedConsultationAsync()
        {
            var consultation = new Consultation(Client.Id, Consultant.Id, new DateTime(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc), 60, null, "Питання для тесту");
            consultation.Confirm(null, DateTime.UtcNow);
            consultation.Complete(DateTime.UtcNow);
            await Consultations.AddAsync(consultation);
            return consultation;
        }
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public FixedClock(DateTime now) => UtcNow = now;
        public DateTime UtcNow { get; }
    }

    private sealed class NoopNotifications : INotificationService
    {
        public Task NotifyAsync(string userId, string title, string body, string? link, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<NotificationDto>> GetMineAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NotificationDto>>(new List<NotificationDto>());

        public Task<Result> MarkReadAsync(string userId, string notificationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
    }

    private sealed class ReviewStore : IReviewRepository
    {
        private readonly List<Review> _items = new();

        public Task<Review?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(r => r.Id == id));

        public Task<IReadOnlyList<Review>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Review>>(_items.ToList());

        public Task AddAsync(Review entity, CancellationToken cancellationToken = default)
        {
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Review entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            _items.RemoveAll(r => r.Id == id);
            return Task.CompletedTask;
        }

        public Task<Review?> GetByConsultationAsync(string consultationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(r => r.ConsultationId == consultationId));

        public Task<IReadOnlyList<Review>> GetAllByConsultantAsync(string consultantId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Review>>(_items.Where(r => r.ConsultantId == consultantId).ToList());

        public Task<(IReadOnlyList<Review> Items, long Total)> GetPageByConsultantAsync(string consultantId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var all = _items.Where(r => r.ConsultantId == consultantId).ToList();
            IReadOnlyList<Review> items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult((items, (long)all.Count));
        }
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
            Task.FromResult(_items.Any(c => c.ConsultantId == consultantId && c.StartUtc == startUtc && c.IsActive()));
    }
}
