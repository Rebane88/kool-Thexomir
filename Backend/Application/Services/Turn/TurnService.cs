using Application.Contracts;
using Application.Services.Turn.DTOs;
using Base.Contracts;

namespace Application.Services.Turn;

public class TurnService(IUnitOfWork unitOfWork, IGameGuard gameGuard) : ITurnService
{
    public Task<Result<TurnAdvancedDto>> EndTurnAsync(Guid gameId, Guid userId)
    {
        throw new NotImplementedException("Pending v6.0 rewrite");
    }
}
