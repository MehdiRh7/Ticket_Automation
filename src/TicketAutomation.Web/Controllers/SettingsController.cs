using Microsoft.AspNetCore.Mvc;
using TicketAutomation.Web.Models;
using TicketAutomation.Web.Services;

namespace TicketAutomation.Web.Controllers;

public sealed class SettingsController(SettingsService settings) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await settings.GetViewModelAsync(ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SettingsViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            await settings.UpdateAsync(model, ct);
            TempData["Success"] = "تنظیمات ذخیره شد.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }
}
