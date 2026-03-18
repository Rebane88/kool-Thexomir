namespace Application.Services.SlotMachine.DTOs;

public class SpinResultDto
{
    public int Outcome { get; set; }
    public int ActionPointsAfter { get; set; }
    public int GoldAfter { get; set; }
    public int GoldSpent { get; set; }
}
