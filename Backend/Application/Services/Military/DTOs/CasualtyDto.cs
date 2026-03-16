namespace Application.Services.Military.DTOs;

public class CasualtyDto
{
    public Guid UnitTypeId { get; set; }
    public string UnitTypeName { get; set; } = string.Empty;
    public int Before { get; set; }
    public int Lost { get; set; }
    public int After { get; set; }
}
