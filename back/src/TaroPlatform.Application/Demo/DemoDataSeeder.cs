using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Domain.Chat;
using TaroPlatform.Domain.Consultations;
using TaroPlatform.Domain.Matrix;
using TaroPlatform.Domain.Reviews;
using TaroPlatform.Domain.Schedule;
using TaroPlatform.Domain.Tarot;
using TaroPlatform.Domain.Users;

namespace TaroPlatform.Application.Demo;

public record DemoSeedResult(
    int Consultants,
    int Clients,
    int Readings,
    int Matrices,
    int Consultations,
    int Conversations,
    int Reviews,
    bool AlreadySeeded);

/// <summary>
/// Тестові дані: 24 таро-консультанти і 76 клієнтів, пароль у всіх — 123456.
/// Містить гадання, матриці, консультації, чати і відгуки. Запускається один раз:
/// якщо demo-користувачі вже є, нічого не створює.
/// </summary>
public class DemoDataSeeder
{
    private const string Password = "123456";
    private const string Domain = "demo.test";
    private const int ConsultantCount = 24;
    private const int ClientCount = 76;

    private static readonly string[] FirstNames =
        { "Олена", "Марія", "Анна", "Ірина", "Наталія", "Оксана", "Катерина", "Світлана", "Юлія", "Вікторія",
          "Андрій", "Олександр", "Дмитро", "Максим", "Богдан", "Тарас", "Ігор", "Сергій", "Роман", "Павло" };

    private static readonly string[] LastNames =
        { "Коваленко", "Шевченко", "Бондаренко", "Ткаченко", "Мельник", "Петренко", "Кравченко", "Савченко",
          "Олійник", "Мороз", "Гриценко", "Лисенко", "Поліщук", "Чорних", "David", "Гордієнко", "Остапенко", "Левченко" };

    private static readonly string[] Bios =
    {
        "Працюю з таро понад кілька років, допомагаю розібратися в стосунках і виборі.",
        "Спокійний і уважний підхід, без категоричних передбачень. Допомагаю побачити варіанти.",
        "Поєдную таро з практикою коучингу. Фокус на кар'єрі та фінансах.",
        "Консультую щодо родинних стосунків і внутрішніх конфліктів.",
        "Матриці долі та таро. Пояснюю просто, без містики заради містики."
    };

    private static readonly string[] Questions =
    {
        "Чи варто змінювати роботу найближчим часом?",
        "Чи буде у мене стабільні стосунки?",
        "Як мені краще діяти в конфлікті з родиною?",
        "Що заважає мені відкладати важливі рішення?",
        "Чи варто інвестувати в нову справу?",
        "Який напрямок навчання мені підходить?",
        "Як знайти баланс між роботою і відпочинком?"
    };

    private static readonly string[] Backgrounds =
    {
        "Останній рік багато змін: переїзд, нова робота і стосунки, які стали напруженими.",
        "Відчуваю, що стою на роздоріжжі, і не можу обрати між двома варіантами.",
        "Працюю в сфері, яка мене виснажує, але гарантований дохід дає відчуття безпеки.",
        "Після розлучення намагаюся знову довіряти і не знаю, з чого почати.",
        "Маю ідею свого бізнесу, але страшно ризикнути заощадженнями."
    };

    private static readonly string[] ChatTexts =
    {
        "Дякую за підтвердження, чекаю на зустріч.",
        "Чи можна додати до консультації ще одне питання?",
        "Звичайно, опишіть, будь ласка, що саме вас турбує.",
        "Дуже вдячна за розмову, все стало зрозумілішим.",
        "Надішлю трохи більше деталей перед консультацією.",
        "Гарного дня, до зустрічі на консультації."
    };

    private static readonly string[] ReviewTexts =
    {
        "Дуже чуйна і детальна консультація, рекомендую.",
        "Все пояснено спокійно, без тиску. Дякую!",
        "Корисно, отримала конкретні поради.",
        "Гарний фахівець, але хотілося б трохи більше часу.",
        "Відповіді дали розуміння, куди рухатися далі."
    };

    private static readonly ReadingTopic[] AllTopics = Enum.GetValues<ReadingTopic>();

    private readonly IUserRepository _users;
    private readonly IRepository<TarotReading> _readings;
    private readonly IRepository<SavedMatrix> _matrices;
    private readonly IRepository<Consultation> _consultations;
    private readonly IRepository<Conversation> _conversations;
    private readonly IRepository<Review> _reviews;
    private readonly IPasswordHasher _hasher;
    private readonly IDateTimeProvider _clock;

    public DemoDataSeeder(
        IUserRepository users,
        IRepository<TarotReading> readings,
        IRepository<SavedMatrix> matrices,
        IRepository<Consultation> consultations,
        IRepository<Conversation> conversations,
        IRepository<Review> reviews,
        IPasswordHasher hasher,
        IDateTimeProvider clock)
    {
        _users = users;
        _readings = readings;
        _matrices = matrices;
        _consultations = consultations;
        _conversations = conversations;
        _reviews = reviews;
        _hasher = hasher;
        _clock = clock;
    }

    public async Task<DemoSeedResult> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _users.GetByEmailAsync($"client1@{Domain}", cancellationToken) is not null)
            return new DemoSeedResult(0, 0, 0, 0, 0, 0, 0, AlreadySeeded: true);

        var random = new Random(2026);
        var now = _clock.UtcNow;
        var hash = _hasher.Hash(Password);

        // Тарологи
        var consultants = new List<Consultant>();
        for (var i = 1; i <= ConsultantCount; i++)
        {
            var consultant = new Consultant($"consultant{i}@{Domain}", hash);
            consultant.SetDisplayName(Name(random));
            consultant.SetBio(Pick(random, Bios));
            consultant.SetPhotoUrl($"https://i.pravatar.cc/300?img={i}");
            consultant.SetSpecializations(PickMany(random, AllTopics, random.Next(1, 4)));
            consultant.SetExperienceYears(random.Next(1, 25));
            consultant.SetLanguages(random.Next(3) == 0 ? new[] { "Українська", "English" } : new[] { "Українська" });
            consultant.SetPriceUah(random.Next(3, 16) * 100);
            consultant.SetSessionMinutes(Pick(random, new[] { 60, 75, 90 }));
            consultant.SetTimezone("Europe/Kyiv");
            consultant.SetSchedule(RandomSchedule(random));
            consultant.SetAcceptingClients(true);
            consultants.Add(consultant);
            await _users.AddAsync(consultant, cancellationToken);
        }

        // Клієнти
        var clients = new List<Client>();
        for (var i = 1; i <= ClientCount; i++)
        {
            var birth = new DateTime(random.Next(1958, 2006), random.Next(1, 13), random.Next(1, 28));
            var client = new Client($"client{i}@{Domain}", hash, birth);
            client.SetDisplayName(Name(random));
            client.SetTimezone("Europe/Kyiv");
            client.SetInterests(PickMany(random, AllTopics, random.Next(1, 4)));
            clients.Add(client);
            await _users.AddAsync(client, cancellationToken);
        }

        // Гадання і матриці
        var readingCount = 0;
        var matrixCount = 0;
        foreach (var client in clients)
        {
            for (var r = random.Next(0, 4); r > 0; r--)
            {
                var topic = Pick(random, AllTopics);
                var question = Pick(random, Questions);
                var reading = new TarotReading(client.Id, topic, Pick(random, Backgrounds), question, DrawCards(random, topic));
                reading.AddMessage(MessageRole.User, question);
                reading.AddMessage(MessageRole.Assistant, "Демо-відповідь: тлумачення подано тимчасово, без AI.", "stub");
                await _readings.AddAsync(reading, cancellationToken);
                readingCount++;
            }

            for (var m = random.Next(0, 3); m > 0; m--)
            {
                var matrix = new SavedMatrix(
                    client.Id,
                    client.BirthDate,
                    random.Next(2) == 0 ? ChannelMethod.Video : ChannelMethod.Document,
                    "Моя матриця");
                if (random.Next(2) == 0)
                {
                    matrix.AddMessage(MatrixMessageRole.User, "Коли мені чекати змін?");
                    matrix.AddMessage(MatrixMessageRole.Assistant, "Демо-відповідь по періодах віку.", null, "stub");
                }
                await _matrices.AddAsync(matrix, cancellationToken);
                matrixCount++;
            }
        }

        // Консультації, чати і відгуки
        var consultationCount = 0;
        var conversationCount = 0;
        var reviewCount = 0;
        var ratings = new Dictionary<string, List<int>>();

        foreach (var client in clients)
        {
            for (var k = random.Next(0, 4); k > 0; k--)
            {
                var consultant = Pick(random, consultants.ToArray());
                var topic = Pick(random, consultant.Specializations.ToArray());
                var question = Pick(random, Questions);
                var roll = random.Next(100);

                if (roll < 40)
                {
                    // Проведена консультація в минулому: є чат і можливий відгук
                    var start = now.Date.AddDays(-random.Next(2, 60)).AddHours(8);
                    var consultation = new Consultation(client.Id, consultant.Id, start, consultant.SessionMinutes, topic, question);
                    consultation.Confirm(null, start);
                    consultation.Complete(now);
                    var conversation = await AddChatAsync(consultation, client, consultant, random, cancellationToken);
                    conversationCount++;
                    await _consultations.AddAsync(consultation, cancellationToken);
                    consultationCount++;

                    if (random.Next(100) < 70)
                    {
                        var rating = random.Next(3, 6);
                        await _reviews.AddAsync(
                            new Review(consultation.Id, client.Id, consultant.Id, rating, Pick(random, ReviewTexts)),
                            cancellationToken);
                        reviewCount++;
                        if (!ratings.TryGetValue(consultant.Id, out var list))
                            ratings[consultant.Id] = list = new List<int>();
                        list.Add(rating);
                    }
                }
                else if (roll < 75)
                {
                    // Підтверджена майбутня консультація з чатом
                    var start = now.Date.AddDays(random.Next(1, 15)).AddHours(8);
                    var consultation = new Consultation(client.Id, consultant.Id, start, consultant.SessionMinutes, topic, question);
                    consultation.Confirm(null, now);
                    await AddChatAsync(consultation, client, consultant, random, cancellationToken);
                    conversationCount++;
                    await _consultations.AddAsync(consultation, cancellationToken);
                    consultationCount++;
                }
                else
                {
                    // Нова заявка, ще не підтверджена
                    var start = now.Date.AddDays(random.Next(2, 20)).AddHours(9);
                    var consultation = new Consultation(client.Id, consultant.Id, start, consultant.SessionMinutes, topic, question);
                    await _consultations.AddAsync(consultation, cancellationToken);
                    consultationCount++;
                }
            }
        }

        // Рейтинг тарологів за відгуками
        foreach (var consultant in consultants)
        {
            if (ratings.TryGetValue(consultant.Id, out var list))
            {
                consultant.SetRating(Math.Round(list.Average(), 2), list.Count);
                await _users.UpdateAsync(consultant, cancellationToken);
            }
        }

        return new DemoSeedResult(
            consultants.Count,
            clients.Count,
            readingCount,
            matrixCount,
            consultationCount,
            conversationCount,
            reviewCount,
            AlreadySeeded: false);
    }

    /// <summary>Створює чат із посиланням на консультацію і кількома репліками.</summary>
    private async Task<Conversation> AddChatAsync(Consultation consultation, Client client, Consultant consultant, Random random, CancellationToken cancellationToken)
    {
        var conversation = new Conversation(client.Id, consultant.Id);
        var link = $"/consultations/{consultation.Id}";

        conversation.AddMessage(new ChatMessage(
            null,
            MessageKind.ConsultationLink,
            $"Консультацію підтверджено. Посилання на консультацію: {link}",
            link));

        for (var i = random.Next(2, 5); i > 0; i--)
        {
            var fromClient = random.Next(2) == 0;
            conversation.AddMessage(new ChatMessage(
                fromClient ? client.Id : consultant.Id,
                MessageKind.Text,
                Pick(random, ChatTexts),
                null));
        }

        consultation.AttachConversation(conversation.Id);
        await _conversations.AddAsync(conversation, cancellationToken);
        return conversation;
    }

    private static List<DrawnCard> DrawCards(Random random, ReadingTopic topic)
    {
        var positions = TopicCatalog.Get(topic).Positions;
        var pool = Enumerable.Range(0, 78).OrderBy(_ => random.Next()).Take(positions.Count).ToList();

        return positions
            .Select((p, i) => new DrawnCard(p.Index, p.Name, pool[i], random.Next(2) == 1))
            .ToList();
    }

    private static List<WorkingInterval> RandomSchedule(Random random)
    {
        var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        var intervals = new List<WorkingInterval>();

        foreach (var day in days.Where(_ => random.Next(10) < 7))
        {
            intervals.Add(new WorkingInterval(day, 600, 780));   // 10:00–13:00
            if (random.Next(2) == 0)
                intervals.Add(new WorkingInterval(day, 960, 1140)); // 16:00–19:00
        }

        if (intervals.Count == 0)
            intervals.Add(new WorkingInterval(DayOfWeek.Monday, 600, 780));

        return intervals;
    }

    private static string Name(Random random) =>
        $"{Pick(random, FirstNames)} {Pick(random, LastNames)}";

    private static T Pick<T>(Random random, IReadOnlyList<T> items) => items[random.Next(items.Count)];

    private static List<T> PickMany<T>(Random random, IReadOnlyList<T> items, int count) =>
        items.OrderBy(_ => random.Next()).Take(count).Distinct().ToList();
}
