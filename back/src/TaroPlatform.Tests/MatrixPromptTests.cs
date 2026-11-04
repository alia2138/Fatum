using TaroPlatform.Application.Matrix;
using TaroPlatform.Application.Tarot;
using Xunit;

namespace TaroPlatform.Tests;

public class MatrixPromptTests
{
    [Fact]
    public void MatrixMode_UsesMatrixPrompt_AndListsPointsWithoutDirection()
    {
        var context = new InterpretationContext(
            "Матриця долі",
            "Дата народження: 17.08.1968",
            "Які мої грошові таланти?",
            new List<InterpretedCard>
            {
                new("Грошовий канал: перша точка (фокус: Зона фінансів)", "Маг", false, "Воля і навички")
            },
            FollowUp: null,
            History: Array.Empty<ChatTurn>(),
            SystemPrompt: MatrixPrompts.System,
            CardsLabel: MatrixPrompts.CardsLabel);

        var messages = TarotPromptBuilder.Build(context);

        Assert.Contains("матриці", messages[0].Text);
        Assert.Contains("Точки матриці долі", messages[1].Text);
        Assert.Contains("фокус: Зона фінансів", messages[1].Text);
        Assert.DoesNotContain("пряма", messages[1].Text);
        Assert.DoesNotContain("Нові карти", messages[1].Text);
    }

    [Fact]
    public void TarotMode_KeepsDefaultPromptAndOrientation()
    {
        var context = new InterpretationContext(
            "Кохання",
            "Передісторія достатньо довга",
            "Чи буде стосунок?",
            new List<InterpretedCard> { new("Минуле", "Туз Жезлів", true, "початок") },
            FollowUp: null,
            History: Array.Empty<ChatTurn>());

        var messages = TarotPromptBuilder.Build(context);

        Assert.Contains("Таро", messages[0].Text);
        Assert.Contains("перевернута", messages[1].Text);
    }
}
