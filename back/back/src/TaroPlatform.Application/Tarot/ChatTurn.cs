namespace TaroPlatform.Application.Tarot;

/// <summary>Одна репліка діалогу у форматі, який розуміють мовні моделі: role = system / user / assistant.</summary>
public record ChatTurn(string Role, string Text);
