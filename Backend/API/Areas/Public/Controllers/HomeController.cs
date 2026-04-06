using Application.Services.Lobby;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Areas.Public.Controllers;

[Area("Public")]
[Authorize(Policy = "PublicAreaPolicy")]
public class HomeController : Controller
{
    // ILobbyService is injected to satisfy MVCINFRA-04 (DI proof-of-concept).
    // It is NOT invoked in Phase 34 — Phase 36 wires up actual lobby flows.
    private readonly ILobbyService _lobbyService;

    public HomeController(ILobbyService lobbyService)
    {
        _lobbyService = lobbyService;
    }

    [AllowAnonymous]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Lobby", new { area = "Public" });
        }
        return View();
    }
}
