namespace Application.Services.GameInitialization.DTOs;

public class KingdomDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public Guid? FactionTypeId { get; set; }
    public string? FactionName { get; set; }
    public bool IsEliminated { get; set; }
    public List<KingdomResourceDto> Resources { get; set; } = [];
}
