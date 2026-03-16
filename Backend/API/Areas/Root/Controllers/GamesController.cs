using Domain.Game;
using Infrastructure;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    private readonly UserManager<AppUser> _userManager;

    public GamesController(ILogger<GamesController> logger, AppDbContext context, UserManager<AppUser> userManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var activeGames = await _context.Games
            .Include(g => g.Kingdoms)
            .Where(g => g.Status == EGameStatus.Lobby || g.Status == EGameStatus.InProgress)
            .AsNoTracking()
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        // Batch-resolve host emails
        var hostUserIds = activeGames
            .Where(g => g.HostUserId.HasValue)
            .Select(g => g.HostUserId!.Value)
            .Distinct()
            .ToList();
        var hostUsers = await _userManager.Users
            .Where(u => hostUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName ?? u.Email ?? string.Empty);

        var viewModels = activeGames.Select(g => new GameListViewModel
        {
            Id = g.Id,
            LobbyCode = g.LobbyCode,
            HostName = g.HostUserId.HasValue && hostUsers.TryGetValue(g.HostUserId.Value, out var name) ? name : null,
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
            .Include(g => g.Kingdoms)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id);

        if (game == null)
            return NotFound();

        string? hostName = null;
        if (game.HostUserId.HasValue)
        {
            var hostUser = await _userManager.FindByIdAsync(game.HostUserId.Value.ToString());
            hostName = hostUser?.UserName ?? hostUser?.Email;
        }

        var vm = new GameListViewModel
        {
            Id = game.Id,
            LobbyCode = game.LobbyCode,
            HostName = hostName,
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
