using Domain.Game;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Areas.Root.Controllers;

[Area("Root")]
[Authorize(Policy = "AdminAreaPolicy")]
public class GameEventsController : Controller
{
    private readonly AppDbContext _context;

    public GameEventsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var events = await _context.GameEvents.AsNoTracking().OrderBy(e => e.Name).ToListAsync();
        return View(events);
    }

    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IFormCollection form)
    {
        var entity = new GameEvent
        {
            Name = form["Name"].ToString(),
            Description = form["Description"].ToString() is { Length: > 0 } desc ? desc : null,
            ResourceEffect = decimal.TryParse(form["ResourceEffect"].ToString(), out var re) ? re : 0m
        };

        _context.GameEvents.Add(entity);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Game Event created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var entity = await _context.GameEvents.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (entity == null)
            return NotFound();

        return View(entity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, IFormCollection form)
    {
        var entity = await _context.GameEvents.FirstOrDefaultAsync(e => e.Id == id);
        if (entity == null)
            return NotFound();

        entity.Name = form["Name"].ToString();
        entity.Description = form["Description"].ToString() is { Length: > 0 } desc ? desc : null;
        entity.ResourceEffect = decimal.TryParse(form["ResourceEffect"].ToString(), out var re) ? re : 0m;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Game Event updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _context.GameEvents.FirstOrDefaultAsync(e => e.Id == id);
        if (entity == null)
            return NotFound();

        _context.GameEvents.Remove(entity);

        try
        {
            await _context.SaveChangesAsync();
            TempData["Success"] = "Game Event deleted successfully.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Cannot delete -- this record is referenced by active data. Remove dependencies first.";
        }

        return RedirectToAction(nameof(Index));
    }
}
