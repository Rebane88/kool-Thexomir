namespace Application.Services.Lobby.DTOs.V1;

public class CreateLobbyResponse
{
    public Guid LobbyId { get; set; }
    public string InviteCode { get; set; } = string.Empty;
}
