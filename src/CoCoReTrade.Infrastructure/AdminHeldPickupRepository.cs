using CoCoReTrade.Application;
using CoCoReTrade.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoCoReTrade.Infrastructure;

public sealed class AdminHeldPickupRepository(AppDbContext db, TimeProvider clock,
    OrderExpirationPolicy expiration) : IAdminHeldPickupRepository
{
    public async Task<IReadOnlyDictionary<string, string>> SellerNamesAsync(IReadOnlyCollection<string> ids, CancellationToken ct)
    {
        var selected = ids.Distinct().Take(300).ToArray();
        return await db.Users.AsNoTracking().Where(user => selected.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.FullName, ct);
    }
    public async Task<IReadOnlyList<HeldPickupOrderDto>> ListAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var cutoff = now.AddMinutes(-expiration.UnpaidReservationMinutes);
        return
        await db.Orders.AsNoTracking()
            .Join(db.Products.AsNoTracking(), order => order.ProductId, product => product.Id,
                (order, product) => new { order, product })
            .Join(db.Users.AsNoTracking(), item => item.order.SellerId, seller => seller.Id,
                (item, seller) => new { item.order, item.product, SellerName = seller.FullName })
            .Where(x => x.product.Status == ProductStatus.Reserved &&
                x.order.Fulfillment == FulfillmentType.Pickup &&
                x.order.PaymentStatus == PaymentStatus.Unpaid &&
                (x.order.Status == OrderStatus.PendingProcessing || x.order.Status == OrderStatus.ReadyForPickup) &&
                (x.order.PickupDeadlineAtUtc != null ? x.order.PickupDeadlineAtUtc > now : x.order.CreatedAtUtc > cutoff))
            .OrderBy(x => x.order.PickupAtUtc)
            .Select(x => new HeldPickupOrderDto(x.order.Id, x.order.ProductId,
                x.order.ProductSku, x.order.ProductName, x.order.RecipientName,
                x.order.RecipientPhone, x.order.PickupAtUtc,
                x.order.PickupDeadlineAtUtc, x.order.Status,
                x.order.OfferId != null, x.order.TotalAmount, x.SellerName, x.order.CreatedAtUtc))
            .Take(300).ToListAsync(ct);
    }
}
