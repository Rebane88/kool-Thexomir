using Application.Services.Lobby.DTOs.V1;

namespace API.Areas.Public.ViewModels;

public class LobbyDetailViewModel
{
    public LobbyResponse Lobby { get; init; }
    public Guid CurrentUserId { get; init; }

    public LobbyDetailViewModel(LobbyResponse lobby, Guid currentUserId)
    {
        Lobby = lobby;
        CurrentUserId = currentUserId;
    }

    public bool IsHost => Lobby.HostUserId.HasValue && Lobby.HostUserId.Value == CurrentUserId;

    public Guid? MyFactionTypeId =>
        Lobby.Players.FirstOrDefault(p => p.UserId == CurrentUserId)?.FactionTypeId;

    public bool AllHaveFactions => Lobby.Players.All(p => p.FactionTypeId.HasValue);

    public bool CanStart => IsHost && Lobby.Players.Count >= 2 && AllHaveFactions;
}
