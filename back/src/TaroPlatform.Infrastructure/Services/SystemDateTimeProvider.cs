using TaroPlatform.Application.Common.Interfaces;

namespace TaroPlatform.Infrastructure.Services;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
