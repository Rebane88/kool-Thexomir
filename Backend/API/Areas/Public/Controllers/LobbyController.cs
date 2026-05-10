using API.Areas.Public.ViewModels;
using API.Extensions;
using API.Hubs;
using Application.Contracts;
using Application.Services.GameHub;
using Application.Services.GameInitialization;
using Application.Services.Lobby;
using Application.Services.Lobby.DTOs.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace API.Areas.Public.Controllers;

[Area("Public")]
[Authorize(Policy = "PublicAreaPolicy")]
public class LobbyController : Controller
{
    private readonly ILobbyService _lobbyService;
    private readonly IHubContext<GameHub, IGameClient> _hubContext;
    private readonly IGameInitializationService _gameInitializationService;
    private readonly IGameLockManager _gameLockManager;

    public LobbyController(
        ILobbyService lobbyService,
        IHubContext<GameHub, IGameClient> hubContext,
        IGameInitializationService gameInitializationService,
        IGameLockManager gameLockManager)
    {
        _lobbyService = lobbyService;
        _hubContext = hubContext;
        _gameInitializationService = gameInitializationService;
        _gameLockManager = gameLockManager;
    }

    // GET /Public/Lobby
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _lobbyService.GetOpenLobbiesAsync();
        var vm = new LobbyIndexViewModel
        {
            OpenLobbies = result.IsSuccess ? result.Value! : new(),
            CreateForm = new CreateLobbyViewModel(),
            JoinForm = new JoinLobbyViewModel()
        };
        return View(vm);
    }

    // POST /Public/Lobby/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "CreateForm")] CreateLobbyViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return await ReRenderIndex(createForm: model, joinForm: new JoinLobbyViewModel());
        }

        var result = await _lobbyService.CreateLobbyAsync(User.UserId(), new CreateLobbyRequest
        {
            MaxPlayers = model.MaxPlayers,
            WinCondition = model.WinCondition
        });

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return await ReRenderIndex(createForm: model, joinForm: new JoinLobbyViewModel());
        }

        return RedirectToAction(nameof(Detail), new { id = result.Value!.LobbyId });
    }

    // POST /Public/Lobby/Join
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join([Bind(Prefix = "JoinForm")] JoinLobbyViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return await ReRenderIndex(createForm: new CreateLobbyViewModel(), joinForm: model);
        }

        var result = await _lobbyService.JoinLobbyAsync(User.UserId(), new JoinLobbyRequest
        {
            InviteCode = model.InviteCode.Trim().ToUpperInvariant()
        });

        if (!result.IsSuccess)
        {
            ModelState.AddModelError($"JoinForm.{nameof(model.InviteCode)}", result.Error!);
            return await ReRenderIndex(createForm: new CreateLobbyViewModel(), joinForm: model);
        }

        await _hubContext.Clients.Group($"game:{result.Value!.Id}")
            .LobbyPlayerJoined(result.Value);

        return RedirectToAction(nameof(Detail), new { id = result.Value!.Id });
    }

    // GET /Public/Lobby/Detail/{id}
    [HttpGet]
    public async Task<IActionResult> Detail(Guid id)
    {
        var result = await _lobbyService.GetLobbyAsync(id);
        if (!result.IsSuccess)
        {
            return NotFound();
        }
        return View(new LobbyDetailViewModel(result.Value!, User.UserId()));
    }

    // POST /Public/Lobby/SelectFaction
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectFaction(SelectFactionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return await ReRenderDetailWithError(model.LobbyId, "Faction selection invalid.");
        }

        var result = await _lobbyService.SelectFactionAsync(User.UserId(), model.LobbyId, model.FactionTypeId);
        if (!result.IsSuccess)
        {
            return await ReRenderDetailWithError(model.LobbyId, result.Error!);
        }

        var lobbyResult = await _lobbyService.GetLobbyAsync(model.LobbyId);
        if (lobbyResult.IsSuccess)
        {
            await _hubContext.Clients.Group($"game:{model.LobbyId}")
                .LobbyFactionSelected(lobbyResult.Value!);
        }

        return RedirectToAction(nameof(Detail), new { id = model.LobbyId });
    }

    // POST /Public/Lobby/Leave/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(Guid id)
    {
        var result = await _lobbyService.LeaveLobbyAsync(User.UserId(), id);
        if (!result.IsSuccess)
        {
            return await ReRenderDetailWithError(id, result.Error!);
        }

        // Broadcast updated lobby to remaining players. If lobby was closed (host left,
        // no successor), GetLobbyAsync will return failure and clients reload to see 404.
        var lobbyResult = await _lobbyService.GetLobbyAsync(id);
        if (lobbyResult.IsSuccess)
        {
            await _hubContext.Clients.Group($"game:{id}")
                .LobbyPlayerLeft(lobbyResult.Value!);
        }

        return RedirectToAction(nameof(Index));
    }

    // POST /Public/Lobby/Start/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(Guid id)
    {
        using var gameLock = await _gameLockManager.AcquireAsync(id);

        var result = await _lobbyService.StartGameAsync(User.UserId(), id);
        if (!result.IsSuccess)
        {
            return await ReRenderDetailWithError(id, result.Error!);
        }

        // 1) Tell clients to navigate to game page
        await _hubContext.Clients.Group($"game:{id}").LobbyGameStarting();

        // 2) Initialize the game world
        var gameState = await _gameInitializationService.InitializeGameAsync(id);

        // 3) Broadcast snapshot to all connected clients
        await _hubContext.Clients.Group($"game:{id}").GameStateSnapshot(gameState);

        // 4) Host follows the redirect (Phase 37 delivers Game/Index)
        return RedirectToAction("Index", "Game", new { area = "Public", id });
    }

    // ----- private helpers -----

    private async Task<IActionResult> ReRenderIndex(CreateLobbyViewModel createForm, JoinLobbyViewModel joinForm)
    {
        var lobbies = await _lobbyService.GetOpenLobbiesAsync();
        return View("Index", new LobbyIndexViewModel
        {
            OpenLobbies = lobbies.IsSuccess ? lobbies.Value! : new(),
            CreateForm = createForm,
            JoinForm = joinForm
        });
    }

    private async Task<IActionResult> ReRenderDetailWithError(Guid lobbyId, string error)
    {
        ModelState.AddModelError(string.Empty, error);
        var lobbyResult = await _lobbyService.GetLobbyAsync(lobbyId);
        if (!lobbyResult.IsSuccess)
        {
            return NotFound();
        }
        return View("Detail", new LobbyDetailViewModel(lobbyResult.Value!, User.UserId()));
    }
}
