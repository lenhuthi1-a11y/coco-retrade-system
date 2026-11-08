using CoCoReTrade.Application;

namespace CoCoReTrade.Tests;

public sealed class PickupSchedulingTests
{
    private readonly ShippingPolicy policy = new(storeAddress: "CoCo Đà Nẵng");
    private static readonly DateTime Now = new(2026, 11, 8, 1, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("2026-11-08T08:30")]
    [InlineData("2026-11-08T21:00")]
    public void AcceptsSameDayDuringOpeningHours(string time)
        => Assert.True(policy.ParsePickupUtc(time, Now) > Now);

    [Theory]
    [InlineData("2026-11-08T08:29")]
    [InlineData("2026-11-08T21:01")]
    [InlineData("2026-11-09T08:30")]
    [InlineData("2026-11-07T15:00")]
    public void RejectsOutsideHoursOrNext24Hours(string time)
        => Assert.Throws<ArgumentException>(() => policy.ParsePickupUtc(time, Now));

    [Fact]
    public void AllowsExactly24HoursButNotAnotherMinute()
    {
        var now = Now.AddHours(2);
        Assert.Equal(now.AddHours(24), policy.ParsePickupUtc("2026-11-09T10:00", now));
        Assert.Throws<ArgumentException>(() => policy.ParsePickupUtc("2026-11-09T10:01", now));
    }

    [Fact]
    public void OfferPickupMustAlsoPrecedeOfferDeadline()
    {
        var deadline = Now.AddHours(3);
        Assert.True(policy.ParseOfferPickupUtc("2026-11-08T10:00", Now, deadline) < deadline);
        Assert.Throws<ArgumentException>(() => policy.ParseOfferPickupUtc("2026-11-08T11:00", Now, deadline));
    }
}
