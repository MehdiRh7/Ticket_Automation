using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace TicketAutomation.Web.Services;

public sealed class AdminAuthOptions
{
    public bool Enabled { get; set; }
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = string.Empty;
}

public sealed class OptionalAdminAuthenticationMiddleware(RequestDelegate next, IOptions<AdminAuthOptions> options)
{
    private readonly AdminAuthOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled || context.User.Identity?.IsAuthenticated == true ||
            context.Request.Path.StartsWithSegments("/Account") ||
            context.Request.Path.StartsWithSegments("/css") ||
            context.Request.Path.StartsWithSegments("/js") ||
            context.Request.Path.StartsWithSegments("/lib"))
        {
            await next(context);
            return;
        }
        context.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(context.Request.Path + context.Request.QueryString)}");
    }
}

public static class AdminSignIn
{
    public static Task SignInAsync(HttpContext context, string username) => context.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }, CookieAuthenticationDefaults.AuthenticationScheme)));
}
