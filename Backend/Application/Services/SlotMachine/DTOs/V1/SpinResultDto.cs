namespace Application.Services.SlotMachine.DTOs.V1;

public class SpinResultDto
{
    public Guid KingdomId { get; set; }
    public int Outcome { get; set; }
    public int ActionPointsAfter { get; set; }
    public int GoldAfter { get; set; }
    public int GoldSpent { get; set; }
}
