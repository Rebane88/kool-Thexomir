using System;
using System.Linq;
using System.Threading.Tasks;
using Infrastructure.Identity;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using API.Areas.Root.ViewModels;

namespace API.Areas.Root.Controllers;

[Area("Root")]
[Authorize(Policy = "AdminAreaPolicy")]
public class UsersController : Controller
{
    private readonly ILogger<UsersController> _logger;
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;


    public UsersController(ILogger<UsersController> logger, AppDbContext context, UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();

        var viewModels = new List<UserListViewModel>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            viewModels.Add(new UserListViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName,
                Roles = roles,
                IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
                LockoutEnd = user.LockoutEnd,
                CreatedAt = user.CreatedAt
            });
        }

        return View(viewModels);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RoleRemove(Guid userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            TempData["Error"] = "User Not Found";
            return RedirectToAction("Index");
        }

        var result = await _userManager.RemoveFromRoleAsync(user, role);
        if (result.Succeeded)
        {
            TempData["Success"] = $"Role '{role}' removed from user.";
            return RedirectToAction("Index");
        }

        TempData["Error"] = result.Errors.Select(e => e.Description).First();
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RoleAdd(Guid userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            TempData["Error"] = "User Not Found";
            return RedirectToAction("Index");
        }

        var result = await _userManager.AddToRoleAsync(user, role);
        if (result.Succeeded)
        {
            TempData["Success"] = $"Role '{role}' added to user.";
            return RedirectToAction("Index");
        }

        TempData["Error"] = result.Errors.Select(e => e.Description).First();
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LockUser(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            TempData["Error"] = "User Not Found";
            return RedirectToAction("Index");
        }

        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        TempData["Success"] = $"User '{user.Email}' has been locked.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlockUser(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            TempData["Error"] = "User Not Found";
            return RedirectToAction("Index");
        }

        await _userManager.SetLockoutEndDateAsync(user, null);

        TempData["Success"] = $"User '{user.Email}' has been unlocked.";
        return RedirectToAction("Index");
    }

}
