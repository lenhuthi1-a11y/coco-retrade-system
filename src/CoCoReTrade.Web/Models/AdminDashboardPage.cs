using CoCoReTrade.Application;

namespace CoCoReTrade.Web.Models;

public sealed record AdminDashboardPage(AdminProductDashboard Products, int HeldPickups,
    int ShippingOrders, int PendingWithdrawals);
