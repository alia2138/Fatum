using TaroPlatform.Application.Common.Interfaces;

namespace TaroPlatform.Infrastructure.Services;

public class SystemRandomSource : IRandomSource
{
    public int Next(int maxExclusive) => Random.Shared.Next(maxExclusive);
}
