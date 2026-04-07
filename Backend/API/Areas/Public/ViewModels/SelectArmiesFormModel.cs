namespace API.Areas.Public.ViewModels;

public class SelectArmiesFormModel
{
    public Guid DeclaredAttackId { get; set; }
    public List<Guid> ArmyIds { get; set; } = new();
}
