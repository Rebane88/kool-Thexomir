using Application.Services.SlotMachine.DTOs.V1;
using Base.Contracts;

namespace Application.Services.SlotMachine;

public interface ISlotMachineService
{
    Task<Result<SpinResultDto>> SpinAsync(Guid gameId, Guid userId);
}
