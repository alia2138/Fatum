namespace TaroPlatform.Domain.Schedule;

/// <summary>
/// Проміжок робочого часу в один день тижня. Час зберігається як хвилини від півночі
/// (наприклад, 10:00 = 600), щоб його легко зберігати і порівнювати.
/// </summary>
public class WorkingInterval
{
    protected WorkingInterval() { }

    public WorkingInterval(DayOfWeek day, int startMinute, int endMinute)
    {
        Day = day;
        StartMinute = startMinute;
        EndMinute = endMinute;
    }

    public DayOfWeek Day { get; private set; }
    public int StartMinute { get; private set; }
    public int EndMinute { get; private set; }

    /// <summary>
    /// Початки слотів на день: від початку інтервалу з кроком у тривалість сесії,
    /// доки сесія повністю вміщується в інтервал.
    /// </summary>
    public static IReadOnlyList<int> GenerateSlotStarts(IEnumerable<WorkingInterval> intervals, DayOfWeek day, int sessionMinutes)
    {
        var result = new List<int>();

        foreach (var interval in intervals.Where(i => i.Day == day).OrderBy(i => i.StartMinute))
        {
            for (var start = interval.StartMinute; start + sessionMinutes <= interval.EndMinute; start += sessionMinutes)
                result.Add(start);
        }

        return result;
    }
}
