namespace Application.Services.Lobby.DTOs;

public class CreateLobbyResponse
{
    public Guid LobbyId { get; set; }
    public string InviteCode { get; set; } = string.Empty;
}
