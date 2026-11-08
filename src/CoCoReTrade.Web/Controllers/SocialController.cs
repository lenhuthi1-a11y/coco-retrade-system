using System.Security.Claims;
using CoCoReTrade.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoCoReTrade.Web.Controllers;

[Authorize]
public sealed class SocialController(ISocialService social) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [HttpPost]
    public async Task<IActionResult> Favorite(Guid productId, bool selected, string? returnUrl, CancellationToken ct)
    {
        var wantsJson = Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);
        try
        {
            var saved = await social.SetFavoriteAsync(UserId, productId, selected, ct);
            if (wantsJson) return Json(new { selected = saved });
        }
        catch (KeyNotFoundException)
        {
            return wantsJson ? NotFound(new { error = "Món đồ không còn mở bán." }) : NotFound();
        }
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action("Index", "Shop")!);
    }
    [HttpPost]
    public async Task<IActionResult> Follow(string sellerId, bool selected, string? returnUrl, CancellationToken ct)
    {
        try { await social.SetFollowAsync(UserId, sellerId, selected, ct); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { TempData["Error"] = ex.Message; }
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action("Index", "Home")!);
    }
}
