using API.Areas.Public.Helpers;
using API.Areas.Public.ViewModels;
using API.Extensions;
using Application.Services.GameInitialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Areas.Public.Controllers;

[Area("Public")]
[Authorize(Policy = "PublicAreaPolicy")]
[Route("Public/Game")]
public class PublicGameController : Controller
{
    private readonly IGameInitializationService _gameInit;

    public PublicGameController(IGameInitializationService gameInit)
    {
        _gameInit = gameInit;
    }

    [HttpGet("Index/{id:guid}")]
    public async Task<IActionResult> Index(Guid id, Guid? selectedTileId)
    {
        var state = await _gameInit.BuildGameStateSnapshotAsync(id);
        var layout = HexLayout.Build(state.Tiles, hexSize: 40);
        var vm = new GameIndexViewModel
        {
            GameId = id,
            MyUserId = User.UserId(),
            SelectedTileId = selectedTileId,
            State = state,
            Layout = layout,
            // Catalog + ArmyTypes left empty — Plans 03/04 fill them
        };
        return View("~/Areas/Public/Views/Game/Index.cshtml", vm);
    }
}
