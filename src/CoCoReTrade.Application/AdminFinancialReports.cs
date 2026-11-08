using CoCoReTrade.Domain;

namespace CoCoReTrade.Application;

public sealed record FinancialSalesTotals(int CompletedOrderCount, decimal CommissionAmount,
    decimal SellerNetAmount, decimal SellerWalletCreditedAmount);
public sealed record FinancialActivityDates(IReadOnlyList<DateTime> PublishedAtUtc, IReadOnlyList<DateTime> SoldAtUtc);
public sealed record FinancialActivityBucket(string Label, int ConsignedCount, int SoldCount);
public sealed record WeeklyTopSeller(string UserId, string Name, int TrustScore, int SoldCount);

public sealed record AdminFinancialReport(string Period, DateOnly FromDate, DateOnly ThroughDate,
    int CompletedOrderCount, decimal CommissionAmount, decimal SellerNetAmount,
    decimal SellerWalletCreditedAmount)
{
    public decimal SellerWalletPendingAmount => Math.Max(0, SellerNetAmount - SellerWalletCreditedAmount);
    public IReadOnlyList<FinancialActivityBucket> Activity { get; init; } = [];
    public IReadOnlyList<WeeklyTopSeller> WeeklyTopSellers { get; init; } = [];
    public DateOnly RankingFromDate { get; init; }
    public DateOnly RankingThroughDate { get; init; }
}

public interface IAdminFinancialReportRepository
{
    Task<FinancialSalesTotals> SalesTotalsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct);
    Task<FinancialActivityDates> ActivityDatesAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct);
    Task<IReadOnlyList<WeeklyTopSeller>> WeeklyTopSellersAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct);
}

public interface IAdminFinancialReportService
{
    Task<AdminFinancialReport> GetAsync(string actorId, string period, DateOnly? referenceDate,
        CancellationToken ct);
}

public sealed class AdminFinancialReportService(IAdminFinancialReportRepository repository,
    IStaffPermissionChecker permissions, TimeProvider clock) : IAdminFinancialReportService
{
    public async Task<AdminFinancialReport> GetAsync(string actorId, string period,
        DateOnly? referenceDate, CancellationToken ct)
    {
        await permissions.EnsureAsync(actorId, StaffPermissions.ManageWallets, ct);
        var zone = VietnamTimeZone();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), zone).DateTime);
        var anchor = referenceDate ?? today;
        var key = period.Trim().ToLowerInvariant();
        var (from, to) = key switch
        {
            "day" => (anchor, anchor.AddDays(1)),
            "week" => WeekBounds(anchor),
            "month" => MonthBounds(anchor),
            _ => throw new ArgumentException("Chọn khoảng thời gian theo ngày, tuần hoặc tháng.", nameof(period))
        };

        var totals = await repository.SalesTotalsAsync(ToUtc(from, zone), ToUtc(to, zone), ct);
        var label = key switch { "day" => "Ngày", "week" => "Tuần", _ => "Tháng" };
        var (rankingFrom, rankingTo) = WeekBounds(anchor);
        var activity = await repository.ActivityDatesAsync(ToUtc(from, zone), ToUtc(to, zone), ct);
        var ranking = await repository.WeeklyTopSellersAsync(ToUtc(rankingFrom, zone), ToUtc(rankingTo, zone), ct);
        var buckets = new List<FinancialActivityBucket>();
        for (var start = from; start < to; start = key == "month" ? start.AddDays(7) : start.AddDays(1))
        {
            var end = key == "month" ? (start.AddDays(7) < to ? start.AddDays(7) : to) : start.AddDays(1);
            var startUtc = ToUtc(start, zone);
            var endUtc = ToUtc(end, zone);
            var labelText = key == "month" ? $"Tuần {buckets.Count + 1}" : start.ToString("dd/MM");
            buckets.Add(new FinancialActivityBucket(labelText,
                activity.PublishedAtUtc.Count(date => date >= startUtc && date < endUtc),
                activity.SoldAtUtc.Count(date => date >= startUtc && date < endUtc)));
        }
        return new AdminFinancialReport(label, from, to.AddDays(-1), totals.CompletedOrderCount,
            totals.CommissionAmount, totals.SellerNetAmount, totals.SellerWalletCreditedAmount)
        {
            Activity = buckets, WeeklyTopSellers = ranking,
            RankingFromDate = rankingFrom, RankingThroughDate = rankingTo.AddDays(-1)
        };
    }

    private DateTime ToUtc(DateOnly date, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), zone);

    private static (DateOnly From, DateOnly To) WeekBounds(DateOnly date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        var monday = date.AddDays(-daysSinceMonday);
        return (monday, monday.AddDays(7));
    }

    private static (DateOnly From, DateOnly To) MonthBounds(DateOnly date)
    {
        var first = new DateOnly(date.Year, date.Month, 1);
        return (first, first.AddMonths(1));
    }

    private static TimeZoneInfo VietnamTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
    }
}
