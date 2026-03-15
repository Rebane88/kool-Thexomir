namespace Application.Services.Lobby.DTOs;

public class PlayerInLobbyDto
{
    public Guid KingdomId { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public Guid? FactionTypeId { get; set; }
    public string? FactionName { get; set; }
    public bool IsHost { get; set; }
}
