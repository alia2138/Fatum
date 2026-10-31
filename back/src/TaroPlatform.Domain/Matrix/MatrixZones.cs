namespace TaroPlatform.Domain.Matrix;

/// <summary>
/// Зони матриці: назва і точки розрахунку, з яких складається відповідь.
/// Прив'язка зон до точок — наша, її можна змінити в одному місці.
/// </summary>
public static class MatrixZones
{
    public sealed record Zone(string Key, string Title, string Description, string[] PointKeys);

    public static readonly IReadOnlyList<Zone> All = new[]
    {
        new Zone("comfort", "Зона комфорту",
            "Центр матриці: стан гармонії та внутрішній ресурс.",
            new[] { "D" }),
        new Zone("talents", "Зона талантів",
            "Небо показує духовні таланти, Земля — матеріальні.",
            new[] { "Heaven", "Earth" }),
        new Zone("finance", "Зона фінансів",
            "Грошовий канал: де і як до людини приходять гроші.",
            new[] { "Money", "MoneyMid", "MoneyEnd" }),
        new Zone("relationships", "Зона стосунків",
            "Точка кохання: яким буде партнер і які стосунки.",
            new[] { "Rel" }),
        new Zone("ancestral", "Зона родових ліній",
            "Родові потенціали батька і матері.",
            new[] { "Zh", "Z" }),
        new Zone("karmic_tail", "Кармічний хвіст",
            "Нижнє число: основна карма, з якою людина прийшла у світ.",
            new[] { "G" }),
        new Zone("visiting_card", "Візитна картка",
            "Перше враження і якості, з якими людина приходить у світ.",
            new[] { "A" }),
        new Zone("higher_self", "Вища суть",
            "Те, що надихає людину і дає їй крила.",
            new[] { "Zh" }),
        new Zone("parents_children", "Зона батьків-дітей",
            "Чоловіча і жіноча лінії: зв'язок із батьками і дітьми.",
            new[] { "Male", "Female" }),
        new Zone("karmic_programs", "Кармічні програми",
            "Родові карми матері і батька, які проходять через життя.",
            new[] { "I", "K" })
    };

    public static Zone? Find(string key) =>
        All.FirstOrDefault(z => string.Equals(z.Key, key, StringComparison.OrdinalIgnoreCase));
}
