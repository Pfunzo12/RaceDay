using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Areas.Participant.Controllers;

public class ProfileController : ParticipantBaseController
{
    public ProfileController(ApiClient api) : base(api) { }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var vm = await LoadAsync();
        return vm == null ? RedirectToAction("Index", "Events") : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var current = await LoadAsync();
            vm.Email = current?.Email ?? "";
            vm.ProfilePicturePath = current?.ProfilePicturePath;
            return View(vm);
        }

        var r = await Api.PutAsync<string>("api/profile",
            new ProfileUpdateRequest(vm.FullName.Trim(), vm.Phone, vm.DateOfBirth, vm.EmergencyContact, vm.ClubName));
        if (r.Ok)
        {
            HttpContext.Session.SetString(SessionKeys.FullName, vm.FullName.Trim());
            SetSuccess("Your profile has been updated.");
        }
        else SetError(r.Error);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Picture(IFormFile? picture)
    {
        if (picture == null || picture.Length == 0)
        {
            SetError("Please choose an image to upload.");
            return RedirectToAction(nameof(Index));
        }
        var r = await Api.UploadAsync("api/profile/picture", picture);
        if (r.Ok) SetSuccess("Profile picture updated."); else SetError(r.Error);
        return RedirectToAction(nameof(Index));
    }

    private async Task<ProfileViewModel?> LoadAsync()
    {
        var r = await Api.GetAsync<ProfileResponse>("api/profile");
        if (!r.Ok || r.Data == null)
        {
            SetError(r.Error);
            return null;
        }
        var p = r.Data;
        return new ProfileViewModel
        {
            FullName = p.FullName, Email = p.Email, Phone = p.Phone, DateOfBirth = p.DateOfBirth,
            EmergencyContact = p.EmergencyContact, ClubName = p.ClubName, ProfilePicturePath = p.ProfilePicturePath
        };
    }
}
