using CoCoReTrade.Application;
using CoCoReTrade.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoCoReTrade.Infrastructure;

public sealed class AdminFinancialReportRepository(AppDbContext db) : IAdminFinancialReportRepository
{
    public async Task<FinancialActivityDates> ActivityDatesAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        var published = await db.Products.AsNoTracking()
            .Where(product => product.PublishedAtUtc >= fromUtc && product.PublishedAtUtc < toUtc)
            .Select(product => product.PublishedAtUtc!.Value).ToListAsync(ct);
        var sold = await db.Orders.AsNoTracking()
            .Where(order => order.Status == OrderStatus.Completed && order.PaymentStatus == PaymentStatus.Paid &&
                order.CompletedAtUtc >= fromUtc && order.CompletedAtUtc < toUtc)
            .Select(order => order.CompletedAtUtc!.Value).ToListAsync(ct);
        return new FinancialActivityDates(published, sold);
    }

    public async Task<IReadOnlyList<WeeklyTopSeller>> WeeklyTopSellersAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        var counts = await db.Orders.AsNoTracking()
            .Where(order => order.Status == OrderStatus.Completed && order.PaymentStatus == PaymentStatus.Paid &&
                order.CompletedAtUtc >= fromUtc && order.CompletedAtUtc < toUtc)
            .GroupBy(order => order.SellerId)
            .Select(group => new { UserId = group.Key, SoldCount = group.Count() })
            .OrderByDescending(row => row.SoldCount).ThenBy(row => row.UserId).Take(10).ToListAsync(ct);
        var ids = counts.Select(row => row.UserId).ToArray();
        var users = await db.Users.AsNoTracking().Where(user => ids.Contains(user.Id))
            .Select(user => new { user.Id, user.FullName, user.TrustScore })
            .ToDictionaryAsync(user => user.Id, ct);
        return counts.Where(row => users.ContainsKey(row.UserId))
            .Select(row => new WeeklyTopSeller(row.UserId, users[row.UserId].FullName,
                users[row.UserId].TrustScore, row.SoldCount)).ToList();
    }

    public async Task<FinancialSalesTotals> SalesTotalsAsync(DateTime fromUtc, DateTime toUtc,
        CancellationToken ct)
    {
        var sales = db.Orders.AsNoTracking().Where(order =>
            order.Status == OrderStatus.Completed && order.PaymentStatus == PaymentStatus.Paid &&
            order.CompletedAtUtc >= fromUtc && order.CompletedAtUtc < toUtc);

        var totals = await sales.GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.Count(),
                Commission = group.Sum(order => order.CommissionAmount),
                SellerNet = group.Sum(order => order.ItemAmount - order.CommissionAmount)
            })
            .FirstOrDefaultAsync(ct);

        var walletCredited = await db.WalletTransactions.AsNoTracking()
            .Where(transaction => transaction.Type == "SellerPayout" && transaction.OrderId.HasValue &&
                sales.Any(order => order.Id == transaction.OrderId!.Value))
            .SumAsync(transaction => (decimal?)transaction.Amount, ct) ?? 0;

        return new FinancialSalesTotals(totals?.Count ?? 0, totals?.Commission ?? 0,
            totals?.SellerNet ?? 0, walletCredited);
    }
}
