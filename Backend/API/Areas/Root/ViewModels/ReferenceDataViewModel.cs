namespace API.Areas.Root.ViewModels;

/// <summary>
/// Marker namespace for reference data view models.
/// The CRUD pattern uses dynamic/anonymous objects via ReferenceDataBaseController.ToViewModel()
/// instead of strongly-typed view models to avoid per-entity duplication.
/// </summary>
public static class ReferenceDataViewModel
{
    // View models for reference data CRUD are returned as anonymous objects from each
    // concrete controller's ToViewModel() override. See ReferenceDataBaseController<TEntity>.
}
