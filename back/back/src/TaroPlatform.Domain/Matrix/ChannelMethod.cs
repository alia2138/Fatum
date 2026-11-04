namespace TaroPlatform.Domain.Matrix;

/// <summary>
/// Спосіб розрахунку грошового каналу і точки кохання.
/// Video — за відеоурок (три грошові точки і точка кохання). Document — за документ (E+V і E+D).
/// </summary>
public enum ChannelMethod
{
    Video = 0,
    Document = 1
}
