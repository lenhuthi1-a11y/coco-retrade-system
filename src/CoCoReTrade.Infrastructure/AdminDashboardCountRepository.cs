using CoCoReTrade.Application;
using CoCoReTrade.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoCoReTrade.Infrastructure;

public sealed class AdminDashboardCountRepository(AppDbContext db, TimeProvider clock,
    OrderExpirationPolicy expiration) : IAdminDashboardCountRepository
{
    public Task<int> HeldPickupsAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var cutoff = now.AddMinutes(-expiration.UnpaidReservationMinutes);
        return db.Orders.AsNoTracking().CountAsync(order =>
            order.Fulfillment == FulfillmentType.Pickup && order.PaymentStatus == PaymentStatus.Unpaid &&
            (order.Status == OrderStatus.PendingProcessing || order.Status == OrderStatus.ReadyForPickup) &&
            (order.PickupDeadlineAtUtc != null ? order.PickupDeadlineAtUtc > now : order.CreatedAtUtc > cutoff) &&
            db.Products.Any(product => product.Id == order.ProductId && product.Status == ProductStatus.Reserved), ct);
    }

    public Task<int> ShippingOrdersAsync(CancellationToken ct) =>
        db.Orders.AsNoTracking().CountAsync(order =>
            (order.Status == OrderStatus.PendingProcessing || order.Status == OrderStatus.Shipping) &&
            order.Fulfillment == FulfillmentType.Shipping, ct);

    public Task<int> PendingWithdrawalsAsync(CancellationToken ct) =>
        db.WithdrawalRequests.AsNoTracking().CountAsync(request =>
            request.Status == "Pending" || request.Status == "Approved", ct);
}
