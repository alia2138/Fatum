namespace TaroPlatform.Application.Common.Interfaces;

/// <summary>
/// Абстракція хешування паролів. Реалізація в Infrastructure.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
