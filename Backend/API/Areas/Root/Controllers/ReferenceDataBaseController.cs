using Base;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Areas.Root.Controllers;

[Area("Root")]
[Authorize(Policy = "AdminAreaPolicy")]
public abstract class ReferenceDataBaseController<TEntity> : Controller
    where TEntity : BaseEntity, IHasName, new()
{
    protected readonly AppDbContext _context;

    protected ReferenceDataBaseController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Returns the DbSet for this entity type.</summary>
    protected abstract DbSet<TEntity> DbSet { get; }

    /// <summary>Human-readable name for display, e.g. "Faction Type".</summary>
    protected abstract string EntityDisplayName { get; }

    /// <summary>Maps flat form fields onto the entity's type-specific properties.</summary>
    protected abstract void PopulateEntity(TEntity entity, IFormCollection form);

    /// <summary>Returns an object with flattened fields suitable for the Edit view.</summary>
    protected abstract object ToViewModel(TEntity entity);

    // -------------------------------------------------------------------------
    // Index
    // -------------------------------------------------------------------------

    public async Task<IActionResult> Index()
    {
        var entities = await DbSet.AsNoTracking().ToListAsync();
        return View(entities);
    }

    // -------------------------------------------------------------------------
    // Create
    // -------------------------------------------------------------------------

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IFormCollection form)
    {
        var entity = new TEntity();
        entity.Name = new LangStr(form["NameEn"].ToString(), "en");
        PopulateEntity(entity, form);

        DbSet.Add(entity);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"{EntityDisplayName} created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // -------------------------------------------------------------------------
    // Edit
    // -------------------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var entity = await DbSet.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (entity == null)
            return NotFound();

        return View(ToViewModel(entity));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, IFormCollection form)
    {
        var entity = await DbSet.FirstOrDefaultAsync(e => e.Id == id);
        if (entity == null)
            return NotFound();

        // Preserve existing translations; update only the English value
        entity.Name.SetTranslation(form["NameEn"].ToString(), "en");
        PopulateEntity(entity, form);

        await _context.SaveChangesAsync();

        TempData["Success"] = $"{EntityDisplayName} updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // -------------------------------------------------------------------------
    // Delete
    // -------------------------------------------------------------------------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await DbSet.FirstOrDefaultAsync(e => e.Id == id);
        if (entity == null)
            return NotFound();

        DbSet.Remove(entity);

        try
        {
            await _context.SaveChangesAsync();
            TempData["Success"] = $"{EntityDisplayName} deleted successfully.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Cannot delete -- this record is referenced by active data. Remove dependencies first.";
        }

        return RedirectToAction(nameof(Index));
    }
}
