using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;

namespace restaurant.Controllers;

public class AccountController : Controller
{
    public IActionResult Login()
    {
        string? redirectUrl = Url.Action("GoogleResponse", "Account");
        AuthenticationProperties properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    public async Task<IActionResult> GoogleResponse()
    {
        var result = await HttpContext.AuthenticateAsync();
        if (!result.Succeeded)
            return RedirectToAction("Login");
        return RedirectToAction("Upload", "Menu");
    }

    public IActionResult Logout()
    {
        return SignOut("Cookies");
    }
}
