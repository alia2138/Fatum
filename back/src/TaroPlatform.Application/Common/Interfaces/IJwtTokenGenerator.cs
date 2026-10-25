using TaroPlatform.Domain.Users;

namespace TaroPlatform.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string Generate(User user);
}
