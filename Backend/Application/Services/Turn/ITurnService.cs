using Application.Services.Turn.DTOs;
using Base.Contracts;

namespace Application.Services.Turn;

public interface ITurnService
{
    Task<Result<TurnAdvancedDto>> EndTurnAsync(Guid gameId, Guid userId);
}
