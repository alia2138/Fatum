namespace TaroPlatform.Domain.Tarot;

/// <summary>
/// Каталог тем і розкладів (патерн Strategy: кожна тема має свою стратегію розкладу).
/// Зберігається в коді, тому змінювати його можна без міграцій.
/// </summary>
public static class TopicCatalog
{
    private static readonly IReadOnlyDictionary<ReadingTopic, SpreadDefinition> Definitions =
        new Dictionary<ReadingTopic, SpreadDefinition>
        {
            [ReadingTopic.Love] = Define(ReadingTopic.Love, "Кохання",
                "Серце завжди говорить тихо, тому карти допоможуть почути те, що ви відчуваєте. Розклад покаже, як складалися стосунки, що відбувається зараз і куди рухається ваша історія.",
                "Минуле стосунків", "Теперішній стан", "Майбутнє"),

            [ReadingTopic.Career] = Define(ReadingTopic.Career, "Кар'єра",
                "Робота це шлях, де кожен крок має значення. Карти покажуть, де ви зараз, які перешкоди з'явилися і що чекає попереду.",
                "Поточна ситуація", "Виклики", "Перспектива"),

            [ReadingTopic.Money] = Define(ReadingTopic.Money, "Фінанси",
                "Фінанси це не лише цифри, а й ставлення до них. Розклад розкриє ваш поточний стан, джерела доходу і те, що їх стримує.",
                "Поточний стан", "Джерела доходу", "Перешкоди", "Порада"),

            [ReadingTopic.Future] = Define(ReadingTopic.Future, "Майбутнє",
                "Майбутнє не записане наперед, але його обриси вже проявляються в теперішньому. Карти покажуть шлях від минулого до можливого результату.",
                "Минуле", "Теперішнє", "Прихований фактор", "Можливий шлях", "Результат"),

            [ReadingTopic.Family] = Define(ReadingTopic.Family, "Сім'я і друзі",
                "У родинних і дружніх стосунках важливі і ваші слова, і те, що відбувається в іншої людини. Розклад допоможе побачити обидві сторони.",
                "Ваша позиція", "Позиція іншого", "Шлях до порозуміння"),

            [ReadingTopic.Study] = Define(ReadingTopic.Study, "Навчання",
                "Навчання це вибір, терпіння і внутрішня мотивація. Карти підкажуть, як ви підходите до навчання, що заважає і чим це закінчиться.",
                "Ваш підхід", "Перешкоди", "Результат"),

            [ReadingTopic.Health] = Define(ReadingTopic.Health, "Здоров'я",
                "Тіло і душа пов'язані між собою. Розклад покаже, на що звернути увагу у фізичному й емоційному стані та як піклуватися про себе.",
                "Тіло", "Душевний стан", "Порада"),

            [ReadingTopic.Creativity] = Define(ReadingTopic.Creativity, "Творчість",
                "Натхнення приходить несподівано. Одна карта, одна ідея: що вона підказує вам у творчому пошуку?",
                "Ключова ідея"),

            [ReadingTopic.Travel] = Define(ReadingTopic.Travel, "Подорож і переїзд",
                "Подорож або переїзд завжди трохи невідомість. Карти розкажуть, що чекає на новому місці, які є ризики і до чого це приведе.",
                "Місце", "Ризики", "Результат"),

            [ReadingTopic.Decision] = Define(ReadingTopic.Decision, "Важливе рішення",
                "Перед важливим вибором часто здається, що всі варіанти однаково складні. Розклад допоможе зважити два шляхи, побачити їхні наслідки і знайти пораду.",
                "Варіант A", "Варіант B", "Наслідки", "Порада")
        };

    public static SpreadDefinition Get(ReadingTopic topic) => Definitions[topic];

    public static IReadOnlyList<SpreadDefinition> All => Definitions.Values.ToList();

    private static SpreadDefinition Define(ReadingTopic topic, string title, string story, params string[] positions) =>
        new(topic, title, story, positions.Select((name, i) => new SpreadPosition(i + 1, name)).ToList());
}
