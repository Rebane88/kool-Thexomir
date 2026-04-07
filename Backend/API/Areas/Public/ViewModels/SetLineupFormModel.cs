namespace API.Areas.Public.ViewModels;

public class SetLineupFormModel
{
    public Guid DeclaredAttackId { get; set; }
    public List<Guid> ArmyIdsInOrder { get; set; } = new();
}
