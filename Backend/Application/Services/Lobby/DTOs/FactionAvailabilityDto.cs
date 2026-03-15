namespace Application.Services.Lobby.DTOs;

public class FactionAvailabilityDto
{
    public Guid FactionTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
}
