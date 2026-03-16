namespace Application.Services.Military.DTOs;

public class TrainTroopsRequest
{
    public Guid BuildingId { get; set; }
    public Guid UnitTypeId { get; set; }
    public int Quantity { get; set; }
}
