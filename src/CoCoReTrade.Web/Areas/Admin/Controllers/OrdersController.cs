using System.Security.Claims;
using CoCoReTrade.Application;
using CoCoReTrade.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoCoReTrade.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = StaffPermissions.ManageOrders)]
public sealed class OrdersController(IAdminOrderService orders, TimeProvider clock,
    OrderExpirationPolicy expiration) : Controller
{
    private string AdminId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [HttpGet]
    public async Task<IActionResult> Index(string? group, string? search, CancellationToken ct)
    {
        var queue = await orders.QueueAsync(AdminId, ct);
        var names = await orders.SellerNamesAsync(AdminId, queue.Select(order => order.SellerId).ToArray(), ct);
        ViewBag.Group = group;
        ViewBag.Search = search;
        ViewBag.SellerNames = names;
        var selected = group switch
        {
            "active" => queue.Where(x => x.Status is OrderStatus.PendingProcessing or OrderStatus.Shipping or OrderStatus.ReadyForPickup).ToList(),
            "shipping" => queue.Where(x => (x.Status is OrderStatus.PendingProcessing or OrderStatus.Shipping) && x.Fulfillment == FulfillmentType.Shipping).ToList(),
            "completed" => queue.Where(x => x.Status == OrderStatus.Completed).ToList(),
            _ => queue
        };
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            selected = selected.Where(order => order.ProductName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                order.ProductSku.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                names.GetValueOrDefault(order.SellerId, "").Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        return View(selected);
    }
    [HttpGet]
    public async Task<IActionResult> HeldPickups(string? search, string? remaining, CancellationToken ct)
    {
        var held = await orders.HeldPickupsAsync(AdminId, ct);
        var now = clock.GetUtcNow().UtcDateTime;
        ViewBag.Search = search;
        ViewBag.Remaining = remaining;
        ViewBag.NowUtc = now;
        ViewBag.ReservationMinutes = expiration.UnpaidReservationMinutes;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            held = held.Where(order => order.ProductName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                order.ProductSku.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                order.SellerName.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        var maxHours = remaining switch { "1h" => 1, "6h" => 6, "24h" => 24, _ => 0 };
        if (maxHours > 0)
            held = held.Where(order => (order.PickupDeadlineAtUtc ??
                order.CreatedAtUtc?.AddMinutes(expiration.UnpaidReservationMinutes) ?? now) <= now.AddHours(maxHours)).ToList();
        return View(held);
    }
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var order = await orders.DetailsAsync(id, AdminId, ct);
        return order is null ? NotFound() : View(order);
    }
    [HttpPost] public Task<IActionResult> Ready(Guid id, string? trackingCode, CancellationToken ct) =>
        Change(id, () => orders.MarkReadyAsync(id, AdminId, ct, trackingCode));
    [HttpPost] public Task<IActionResult> Confirm(Guid id, CancellationToken ct) =>
        Change(id, () => orders.ConfirmAsync(id, AdminId, ct));
    [HttpPost] public Task<IActionResult> ConfirmPayment(Guid id, CancellationToken ct) =>
        Change(id, () => orders.ConfirmPaymentAsync(id, AdminId, null, ct));
    [HttpPost] public Task<IActionResult> Complete(Guid id, CancellationToken ct) =>
        Change(id, () => orders.CompleteAsync(id, AdminId, ct));
    [HttpPost] public Task<IActionResult> Cancel(Guid id, CancellationToken ct) =>
        Change(id, () => orders.CancelAsync(id, AdminId, ct));
    [HttpPost] public Task<IActionResult> ReturnUndelivered(Guid id, bool itemReturnedToStore, CancellationToken ct) =>
        Change(id, () => orders.ReturnUndeliveredAsync(id, AdminId, itemReturnedToStore, ct));
    private async Task<IActionResult> Change(Guid id, Func<Task<OrderDto>> action)
    {
        try { await action(); TempData["Message"] = "Đã cập nhật đơn hàng."; }
        catch (Exception ex) when (ex is KeyNotFoundException or ArgumentException or InvalidOperationException)
        { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }
}
