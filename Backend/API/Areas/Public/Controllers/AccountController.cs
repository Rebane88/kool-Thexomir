using API.Areas.Public.ViewModels;
using Application.Services.Auth;
using Application.Services.Auth.DTOs;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Areas.Public.Controllers;

[Area("Public")]
[Authorize(Policy = "PublicAreaPolicy")]
public class AccountController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signIn;
    private readonly IAuthService _authService;

    public AccountController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signIn,
        IAuthService authService)
    {
        _userManager = userManager;
        _signIn = signIn;
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authService.RegisterAsync(new RegisterRequest
        {
            Email = model.Email,
            Password = model.Password
        });

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Account creation succeeded but user lookup failed.");
            return View(model);
        }

        var principal = await _signIn.CreateUserPrincipalAsync(user);
        await HttpContext.SignInAsync("PublicCookie", principal, new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });

        return RedirectToAction("Index", "Home", new { area = "Public" });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        model.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
        {
            ModelState.AddModelError(string.Empty, "Invalid credentials.");
            return View(model);
        }

        var principal = await _signIn.CreateUserPrincipalAsync(user);
        await HttpContext.SignInAsync("PublicCookie", principal, new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });

        var redirectUrl = returnUrl ?? Url.Action("Index", "Home", new { area = "Public" })!;
        return LocalRedirect(redirectUrl);
    }
}
