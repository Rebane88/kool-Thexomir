using Application.Services.Lobby.DTOs.V1;

namespace API.Areas.Public.ViewModels;

public class LobbyIndexViewModel
{
    public List<LobbyResponse> OpenLobbies { get; set; } = new();
    public CreateLobbyViewModel CreateForm { get; set; } = new();
    public JoinLobbyViewModel JoinForm { get; set; } = new();
}
