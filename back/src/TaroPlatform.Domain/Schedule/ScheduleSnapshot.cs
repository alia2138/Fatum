namespace TaroPlatform.Domain.Schedule;

/// <summary>Попередній графік таролога з періодом, коли він діяв.</summary>
public class ScheduleSnapshot
{
    protected ScheduleSnapshot() { }

    public ScheduleSnapshot(DateTime validFrom, DateTime validTo, string? timezone, List<WorkingInterval> intervals)
    {
        ValidFrom = validFrom;
        ValidTo = validTo;
        Timezone = timezone;
        Intervals = intervals;
    }

    public DateTime ValidFrom { get; private set; }
    public DateTime ValidTo { get; private set; }
    public string? Timezone { get; private set; }
    public List<WorkingInterval> Intervals { get; private set; } = new();
}
