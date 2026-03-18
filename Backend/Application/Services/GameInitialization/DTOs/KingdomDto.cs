namespace Application.Services.GameInitialization.DTOs;

public class KingdomDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public Guid FactionTypeId { get; set; }
    public string? FactionName { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<KingdomResourceDto> Resources { get; set; } = [];
}
