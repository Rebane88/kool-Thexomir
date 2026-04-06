using API.Areas.Public.Helpers;
using API.Areas.Public.ViewModels;
using API.Extensions;
using API.Hubs;
using Application.Contracts;
using Application.Services.Abandon;
using Application.Services.Army;
using Application.Services.Army.DTOs;
using Application.Services.Building;
using Application.Services.Building.DTOs;
using Application.Services.GameHub;
using Application.Services.GameInitialization;
using Application.Services.SlotMachine;
using Application.Services.Turn;
using Application.Services.Turn.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace API.Areas.Public.Controllers;

[Area("Public")]
[Authorize(Policy = "PublicAreaPolicy")]
[Route("Public/Game")]
public class PublicGameController : Controller
{
    private readonly IGameInitializationService _gameInit;
    private readonly IBuildingService _buildingService;
    private readonly IArmyService _armyService;
    private readonly ISlotMachineService _slotMachineService;
    private readonly ITurnService _turnService;
    private readonly IAbandonService _abandonService;
    private readonly IGameLockManager _gameLockManager;
    private readonly IHubContext<GameHub, IGameClient> _hubContext;

    public PublicGameController(
        IGameInitializationService gameInit,
        IBuildingService buildingService,
        IArmyService armyService,
        ISlotMachineService slotMachineService,
        ITurnService turnService,
        IAbandonService abandonService,
        IGameLockManager gameLockManager,
        IHubContext<GameHub, IGameClient> hubContext)
    {
        _gameInit = gameInit;
        _buildingService = buildingService;
        _armyService = armyService;
        _slotMachineService = slotMachineService;
        _turnService = turnService;
        _abandonService = abandonService;
        _gameLockManager = gameLockManager;
        _hubContext = hubContext;
    }

    [HttpGet("Index/{id:guid}")]
    public async Task<IActionResult> Index(Guid id, Guid? selectedTileId)
    {
        var state = await _gameInit.BuildGameStateSnapshotAsync(id);
        var buildingTypes = (await _buildingService.GetBuildingTypesAsync(id, User.UserId())).ToList();
        var layout = HexLayout.Build(state.Tiles, hexSize: 40);
        var vm = new GameIndexViewModel
        {
            GameId = id,
            MyUserId = User.UserId(),
            SelectedTileId = selectedTileId,
            State = state,
            Layout = layout,
            Catalog = BuildCatalog(buildingTypes, state, User.UserId()),
            ArmyTypes = (await _armyService.GetArmyTypesAsync(id, User.UserId())).ToList(),
        };
        return View("~/Areas/Public/Views/Game/Index.cshtml", vm);
    }

    [HttpPost("{id:guid}/Build")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Build(Guid id, PlaceBuildingFormModel form)
    {
        if (!ModelState.IsValid)
        {
            TempData["BuildError"] = "Invalid form.";
            return RedirectToAction(nameof(Index), new { id, selectedTileId = form.TileId });
        }

        using var gameLock = await _gameLockManager.AcquireAsync(id);
        var result = await _buildingService.PlaceBuildingAsync(id, User.UserId(),
            new PlaceBuildingRequest
            {
                TileId = form.TileId,
                BuildingTypeId = form.BuildingTypeId
            });

        if (!result.IsSuccess)
        {
            TempData["BuildError"] = result.Error;
            return RedirectToAction(nameof(Index), new { id, selectedTileId = form.TileId });
        }

        await _hubContext.Clients.Group($"game:{id}").BuildingPlaced(result.Value!);
        return RedirectToAction(nameof(Index), new { id, selectedTileId = form.TileId });
    }

    [HttpPost("{id:guid}/Train")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Train(Guid id, TrainArmyFormModel form, Guid? selectedTileId)
    {
        if (!ModelState.IsValid)
        {
            TempData["TrainError"] = "Invalid form.";
            return RedirectToAction(nameof(Index), new { id, selectedTileId });
        }

        using var gameLock = await _gameLockManager.AcquireAsync(id);
        var result = await _armyService.TrainArmyAsync(id, User.UserId(),
            new TrainArmyRequest
            {
                BuildingId = form.BuildingId,
                ArmyTypeId = form.ArmyTypeId
            });

        if (!result.IsSuccess)
        {
            TempData["TrainError"] = result.Error;
            return RedirectToAction(nameof(Index), new { id, selectedTileId });
        }

        await _hubContext.Clients.Group($"game:{id}").ArmyTrained(result.Value!);
        return RedirectToAction(nameof(Index), new { id, selectedTileId });
    }

    [HttpPost("{id:guid}/Spin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Spin(Guid id, Guid? selectedTileId)
    {
        using var gameLock = await _gameLockManager.AcquireAsync(id);
        var result = await _slotMachineService.SpinAsync(id, User.UserId());
        if (!result.IsSuccess)
        {
            TempData["SpinError"] = result.Error;
            return RedirectToAction(nameof(Index), new { id, selectedTileId });
        }
        await _hubContext.Clients.Group($"game:{id}").SlotMachineSpun(result.Value!);
        TempData["SpinOutcome"] = result.Value!.Outcome;
        TempData["SpinApAfter"] = result.Value!.ActionPointsAfter;
        TempData["SpinGoldAfter"] = result.Value!.GoldAfter;
        TempData["SpinGoldSpent"] = result.Value!.GoldSpent;
        return RedirectToAction(nameof(Index), new { id, selectedTileId });
    }

    [HttpPost("{id:guid}/EndTurn")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EndTurn(Guid id)
    {
        using var gameLock = await _gameLockManager.AcquireAsync(id);

        var result = await _turnService.EndTurnAsync(
            id,
            User.UserId(),
            onRoundResolved: async (round, battleId) =>
                await _hubContext.Clients.Group($"game:{id}").BattleRoundResolved(round),
            onBattleResolved: async (battleResult) =>
                await _hubContext.Clients.Group($"game:{id}").BattleResolved(battleResult));

        if (!result.IsSuccess)
        {
            TempData["EndTurnError"] = result.Error;
            return RedirectToAction(nameof(Index), new { id });
        }

        await _hubContext.Clients.Group($"game:{id}").TurnAdvanced(result.Value!);

        if (result.Value!.PhaseChanged)
        {
            await _hubContext.Clients.Group($"game:{id}")
                .PhaseChanged(new PhaseChangedDto
                {
                    Phase = result.Value!.CurrentPhase,
                    PreviousPhase = "Action",
                    RoundNumber = result.Value!.RoundNumber
                });
        }

        if (result.Value!.GameOver is not null)
            await _hubContext.Clients.Group($"game:{id}").GameOver(result.Value!.GameOver);

        return RedirectToAction(nameof(Index), new { id });
    }

    [HttpPost("{id:guid}/Abandon")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Abandon(Guid id)
    {
        using var gameLock = await _gameLockManager.AcquireAsync(id);
        var result = await _abandonService.AbandonGameAsync(id, User.UserId());
        if (!result.IsSuccess)
        {
            TempData["AbandonError"] = result.Error;
            return RedirectToAction(nameof(Index), new { id });
        }
        if (result.Value is not null)
        {
            await _hubContext.Clients.Group($"game:{id}").GameOver(result.Value);
        }
        return RedirectToAction("Index", "Lobby", new { area = "Public" });
    }

    private static List<BuildingCatalogEntryViewModel> BuildCatalog(
        List<BuildingTypeDto> buildingTypes, Application.Services.GameInitialization.DTOs.GameStateDto state, Guid userId)
    {
        var myKingdom = state.Kingdoms.FirstOrDefault(k => k.UserId == userId);
        if (myKingdom is null) return [];

        var resources = myKingdom.Resources.ToDictionary(r => r.ResourceType, r => r.Amount);
        var ownedBuildingTypeIds = state.Tiles
            .Where(t => t.KingdomId == myKingdom.Id)
            .SelectMany(t => t.Buildings.Select(b => b.BuildingTypeId))
            .ToHashSet();

        return buildingTypes.Select(bt => new BuildingCatalogEntryViewModel
        {
            BuildingType = bt,
            CanAfford =
                resources.GetValueOrDefault("Gold") >= bt.CostGold &&
                resources.GetValueOrDefault("Food") >= bt.CostFood &&
                resources.GetValueOrDefault("Wood") >= bt.CostWood &&
                resources.GetValueOrDefault("Stone") >= bt.CostStone &&
                resources.GetValueOrDefault("Mana") >= bt.CostMana,
            PrereqMet =
                !bt.UnlockedByBuildingTypeId.HasValue ||
                ownedBuildingTypeIds.Contains(bt.UnlockedByBuildingTypeId.Value)
        }).ToList();
    }
}
