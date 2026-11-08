namespace CoCoReTrade.Application;

public interface IAdminDashboardCountRepository
{
    Task<int> HeldPickupsAsync(CancellationToken ct);
    Task<int> ShippingOrdersAsync(CancellationToken ct);
    Task<int> PendingWithdrawalsAsync(CancellationToken ct);
}
