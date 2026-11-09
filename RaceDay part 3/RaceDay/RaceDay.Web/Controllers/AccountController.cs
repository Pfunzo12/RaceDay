using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : AppController
{
    public AccountController(ApiClient api) : base(api) { }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null, bool expired = false)
    {
        if (HttpContext.Session.IsAuthenticated()) return RedirectToRoleHome();
        if (expired) ViewData["Info"] = "Your session ended. Please log in again.";
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await Api.LoginAsync(vm.Email.Trim(), vm.Password);
        if (!result.Ok)
        {
            ModelState.AddModelError("", result.Error ?? "We couldn't sign you in.");
            return View(vm);
        }

        if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
            return LocalRedirect(vm.ReturnUrl);
        return RedirectToRoleHome();
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.IsAuthenticated()) return RedirectToRoleHome();
        return View(new RegisterViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (vm.Role != AppRoles.Organiser && vm.Role != AppRoles.Participant)
            ModelState.AddModelError(nameof(vm.Role), "Please choose participant or organiser.");
        if (!ModelState.IsValid) return View(vm);

        var result = await Api.PostAsync<string>("api/auth/register",
            new RegisterRequest(vm.FullName.Trim(), vm.Email.Trim(), vm.Password, vm.Role));
        if (!result.Ok)
        {
            ModelState.AddModelError("", result.Error ?? "We couldn't create your account.");
            return View(vm);
        }

        SetSuccess("Your account is ready. Please log in.");
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await Api.LogoutAsync();
        TempData["Success"] = "You have been logged out. See you at the start line!";
        return RedirectToAction("Index", "Home");
    }
}
