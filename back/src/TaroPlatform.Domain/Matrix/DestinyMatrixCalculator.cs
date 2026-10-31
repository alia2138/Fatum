using TaroPlatform.Domain.Tarot;

namespace TaroPlatform.Domain.Matrix;

/// <summary>
/// Розрахунок матриці долі за правилом 22: якщо число більше 22, сума його цифр
/// додається знову, доки не стане 22 або менше.
/// </summary>
public static class DestinyMatrixCalculator
{
    private const int MaxValue = 22;
    private const int CycleYears = 80;
    private const double StepYears = 2.5;

    /// <summary>Вікові точки кожні 10 років: 0, 10, ..., 80 (80 років повертає до точки А).</summary>
    private static readonly string[] AnchorKeys = { "A", "Zh", "B", "Z", "V", "I", "G", "K", "A" };

    /// <summary>Усі точки матриці з назвою і групою. Точки каналу відео є лише в режимі Video.</summary>
    public static readonly (string Key, string Title, string Group)[] PointDefinitions =
    {
        ("A", "Візитна картка (день народження)", "Основні"),
        ("B", "Ангел-охоронець (місяць народження)", "Основні"),
        ("V", "Матеріальна карма (рік народження)", "Основні"),
        ("G", "Нижнє число (основна карма)", "Основні"),
        ("D", "Центр (комфорт душі)", "Основні"),
        ("Zh", "Родовий потенціал батька", "Родові"),
        ("Z", "Родовий потенціал матері", "Родові"),
        ("I", "Родова карма матері", "Родові"),
        ("K", "Родова карма батька", "Родові"),
        ("Heaven", "Небо (Б+Г)", "Призначення"),
        ("Earth", "Земля (А+В)", "Призначення"),
        ("Personal", "Особисте призначення (до 40 років)", "Призначення"),
        ("Male", "Чоловіча лінія (Ж+И)", "Призначення"),
        ("Female", "Жіноча лінія (З+К)", "Призначення"),
        ("Social", "Соціальне призначення (40–60 років)", "Призначення"),
        ("Spiritual", "Духовне призначення (після 60 років)", "Призначення"),
        ("E", "Вхід у канал стосунків і грошей", "Канал"),
        ("Rel", "Точка стосунків / кохання", "Канал"),
        ("Money", "Точка грошей (перша)", "Канал"),
        ("MoneyMid", "Грошовий канал: друга точка", "Канал (відео)"),
        ("MoneyEnd", "Грошовий канал: третя точка", "Канал (відео)")
    };

    public static DestinyMatrix Calculate(DateOnly birthDate, ChannelMethod method = ChannelMethod.Video)
    {
        var a = Reduce(birthDate.Day);
        var b = birthDate.Month;
        var v = Reduce(DigitSum(birthDate.Year));

        var g = Reduce(a + b + v);
        var d = Reduce(a + b + v + g);

        var zh = Reduce(a + b);
        var z = Reduce(b + v);
        var i = Reduce(v + g);
        var k = Reduce(g + a);

        var e = Reduce(d + g);

        var heaven = Reduce(b + g);
        var earth = Reduce(a + v);
        var personal = Reduce(heaven + earth);
        var male = Reduce(zh + i);
        var female = Reduce(z + k);
        var social = Reduce(male + female);
        var spiritual = Reduce(personal + social);

        var points = new Dictionary<string, int>
        {
            ["A"] = a, ["B"] = b, ["V"] = v, ["G"] = g, ["D"] = d,
            ["Zh"] = zh, ["Z"] = z, ["I"] = i, ["K"] = k,
            ["E"] = e,
            ["Heaven"] = heaven, ["Earth"] = earth, ["Personal"] = personal,
            ["Male"] = male, ["Female"] = female, ["Social"] = social, ["Spiritual"] = spiritual
        };

        if (method == ChannelMethod.Document)
        {
            // Документ: стосунки = E+V, гроші = E+D
            points["Rel"] = Reduce(e + v);
            points["Money"] = Reduce(e + d);
        }
        else
        {
            // Відео: центр+нижнє число (12), точка кохання = центр + 12,
            // грошовий канал = B+V, далі додаємо 12 і нижнє число.
            var center12 = Reduce(d + g);
            var money1 = Reduce(b + v);

            points["Rel"] = Reduce(d + center12);
            points["Money"] = money1;
            points["MoneyMid"] = Reduce(center12 + money1);
            points["MoneyEnd"] = Reduce(Reduce(money1 + g) + g);
        }

        return new DestinyMatrix(birthDate, points);
    }

    /// <summary>Правило 22: згортає число сумою цифр, доки воно не стане 22 або менше.</summary>
    public static int Reduce(int number)
    {
        var n = Math.Abs(number);
        while (n > MaxValue)
            n = DigitSum(n);
        return n;
    }

    public static int DigitSum(int number)
    {
        var n = Math.Abs(number);
        var sum = 0;
        while (n > 0)
        {
            sum += n % 10;
            n /= 10;
        }
        return sum;
    }

    /// <summary>
    /// Значення в будь-якій точці віку (крок 2,5 року).
    /// Кожні 10 років — базові точки; кожні 5 — сума сусідніх базових;
    /// проміжні 2,5 — сума сусідніх значень з кроком 2,5.
    /// </summary>
    public static int ValueAt(DestinyMatrix matrix, double age)
    {
        var t = age % CycleYears;
        if (t < 0)
            t += CycleYears;

        if (t % 10 == 0)
            return AnchorAt(matrix, (int)t);

        if (t % 5 == 0)
            return Reduce(AnchorAt(matrix, (int)(t - 5)) + AnchorAt(matrix, (int)(t + 5)));

        return Reduce(ValueAt(matrix, t - StepYears) + ValueAt(matrix, t + StepYears));
    }

    /// <summary>Період, у який потрапляє вік (2,5-річний відрізок).</summary>
    public static AgePeriod PeriodAt(DestinyMatrix matrix, double age)
    {
        var from = Math.Floor(age / StepYears) * StepYears;
        return new AgePeriod(from, from + StepYears, ValueAt(matrix, from));
    }

    /// <summary>Повна шкала від 0 до 80 років.</summary>
    public static IReadOnlyList<AgePeriod> Timeline(DestinyMatrix matrix) =>
        Enumerable.Range(0, (int)(CycleYears / StepYears))
            .Select(i => PeriodAt(matrix, i * StepYears))
            .ToList();

    /// <summary>Яка лінія призначення діє у цьому віці.</summary>
    public static string LineFor(double age)
    {
        var t = age % CycleYears;
        if (t < 20) return "Формування особистості";
        if (t < 40) return "Особисте призначення";
        if (t < 60) return "Соціальне призначення";
        return "Духовне призначення";
    }

    /// <summary>Старший аркан для значення точки. 22 — це Дурень (0).</summary>
    public static TarotCard ArcanaFor(int value) => TarotDeck.GetById(value % MaxValue);

    private static int AnchorAt(DestinyMatrix matrix, int years) =>
        matrix.Points[AnchorKeys[years / 10]];
}
