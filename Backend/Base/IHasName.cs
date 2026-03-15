namespace Base;

/// <summary>
/// Marks an entity that has a localised Name (LangStr).
/// Used as a constraint on the generic reference data admin controller
/// so the base class can handle Name creation/update without reflection.
/// </summary>
public interface IHasName
{
    LangStr Name { get; set; }
}
