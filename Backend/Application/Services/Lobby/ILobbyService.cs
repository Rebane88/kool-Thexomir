using Application.Services.Lobby.DTOs.V1;
using Base.Contracts;

namespace Application.Services.Lobby;

public interface ILobbyService
{
    Task<Result<CreateLobbyResponse>> CreateLobbyAsync(Guid userId, CreateLobbyRequest request);
    Task<Result<LobbyResponse>> JoinLobbyAsync(Guid userId, JoinLobbyRequest request);
    Task<Result<bool>> LeaveLobbyAsync(Guid userId, Guid lobbyId);
    Task<Result<bool>> SelectFactionAsync(Guid userId, Guid lobbyId, Guid factionTypeId);
    Task<Result<bool>> StartGameAsync(Guid userId, Guid lobbyId);
    Task<Result<LobbyResponse>> GetLobbyAsync(Guid lobbyId);
    Task<Result<List<LobbyResponse>>> GetOpenLobbiesAsync();
}
