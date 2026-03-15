using System;
using System.Linq;
using System.Threading.Tasks;
using Domain.Game;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using API.Areas.Root.ViewModels;

namespace API.Areas.Root.Controllers;

[Area("Root")]
[Authorize(Policy = "AdminAreaPolicy")]
public class GamesController : Controller
{
    private readonly ILogger<GamesController> _logger;
    private readonly AppDbContext _context;

    public GamesController(ILogger<GamesController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var activeGames = await _context.Games
            .Include(g => g.HostUser)
            .Include(g => g.Kingdoms)
            .Where(g => g.Status == EGameStatus.Lobby || g.Status == EGameStatus.InProgress)
            .AsNoTracking()
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        var viewModels = activeGames.Select(g => new GameListViewModel
        {
            Id = g.Id,
            LobbyCode = g.LobbyCode,
            HostName = g.HostUser?.UserName ?? g.HostUser?.Email,
            PlayerCount = g.Kingdoms?.Count(k => k.AppUserId.HasValue) ?? 0,
            MaxPlayers = g.MaxPlayers,
            Status = g.Status,
            CreatedAt = g.CreatedAt
        }).ToList();

        return View(viewModels);
    }

    [HttpGet]
    public async Task<IActionResult> ForceClose(Guid id)
    {
        var game = await _context.Games
            .Include(g => g.HostUser)
            .Include(g => g.Kingdoms)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id);

        if (game == null)
            return NotFound();

        var vm = new GameListViewModel
        {
            Id = game.Id,
            LobbyCode = game.LobbyCode,
            HostName = game.HostUser?.UserName ?? game.HostUser?.Email,
            PlayerCount = game.Kingdoms?.Count(k => k.AppUserId.HasValue) ?? 0,
            MaxPlayers = game.MaxPlayers,
            Status = game.Status,
            CreatedAt = game.CreatedAt
        };

        return View(vm);
    }

    [HttpPost, ActionName("ForceClose")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForceCloseConfirmed(Guid id)
    {
        // AsTracking() required — the DbContext default is NoTrackingWithIdentityResolution
        // which means FindAsync returns a detached entity and SaveChangesAsync generates no UPDATE.
        var game = await _context.Games
            .AsTracking()
            .FirstOrDefaultAsync(g => g.Id == id);

        if (game == null)
            return NotFound();

        game.Status = EGameStatus.Completed;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Game {game.LobbyCode} force-closed.";
        return RedirectToAction("Index");
    }
}
