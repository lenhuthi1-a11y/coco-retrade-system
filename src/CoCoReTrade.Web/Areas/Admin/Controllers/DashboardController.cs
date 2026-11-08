using System.Security.Claims;
using CoCoReTrade.Application;
using CoCoReTrade.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoCoReTrade.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = StaffPermissions.ReadPending)]
public sealed class DashboardController(IAdminProductService products,
    IAdminDashboardCountRepository counts, IAuthorizationService authorization) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var productCounts = await products.DashboardAsync(actorId, ct);
        var held = 0;
        var shipping = 0;
        var pendingWithdrawals = 0;
        if ((await authorization.AuthorizeAsync(User, StaffPermissions.ManageOrders)).Succeeded)
        {
            held = await counts.HeldPickupsAsync(ct);
            shipping = await counts.ShippingOrdersAsync(ct);
        }
        if ((await authorization.AuthorizeAsync(User, StaffPermissions.ManageWallets)).Succeeded)
            pendingWithdrawals = await counts.PendingWithdrawalsAsync(ct);
        return View(new AdminDashboardPage(productCounts, held, shipping, pendingWithdrawals));
    }
}
