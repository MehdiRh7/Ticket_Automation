using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TicketAutomation.Web.Models;
using TicketAutomation.Web.Services;

namespace TicketAutomation.Web.Controllers;

public sealed class AccountController(IOptions<AdminAuthOptions> options) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var config = options.Value;
        if (!config.Enabled) return RedirectToAction("Index", "Home");
        if (FixedEquals(model.Username, config.Username) && FixedEquals(model.Password, config.Password))
        {
            await AdminSignIn.SignInAsync(HttpContext, config.Username);
            return LocalRedirect(Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl! : "/");
        }
        ModelState.AddModelError(string.Empty, "نام کاربری یا رمز عبور نادرست است.");
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private static bool FixedEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(left)), SHA256.HashData(Encoding.UTF8.GetBytes(right)));
}
