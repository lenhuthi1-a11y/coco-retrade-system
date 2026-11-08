using CoCoReTrade.Application;
using CoCoReTrade.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoCoReTrade.Infrastructure;

public sealed class UserProfileRepository(AppDbContext db) : IUserProfileRepository
{
    public Task<UserProfileDto?> GetAsync(string userId, CancellationToken ct) =>
        db.Users.AsNoTracking().Where(x => x.Id == userId)
            .Select(x => new UserProfileDto(x.Id, x.FullName, x.TrustScore, x.AvatarStorageKey != null,
                db.Orders.Count(order => order.SellerId == x.Id && order.Status == OrderStatus.Completed),
                db.Products.Count(product => product.SellerId == x.Id &&
                    (product.Status == ProductStatus.Active || product.Status == ProductStatus.Reserved || product.Status == ProductStatus.Sold))))
            .FirstOrDefaultAsync(ct);
    public Task<AvatarLocation?> AvatarAsync(string userId, CancellationToken ct) =>
        db.Users.AsNoTracking().Where(x => x.Id == userId && x.AvatarStorageKey != null && x.AvatarContentType != null)
            .Select(x => new AvatarLocation(x.AvatarStorageKey!, x.AvatarContentType!)).FirstOrDefaultAsync(ct);
    public async Task<string?> SetAvatarAsync(string userId, string key, string contentType, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct) ??
            throw new KeyNotFoundException("Tài khoản không tồn tại.");
        var previous = user.AvatarStorageKey;
        user.AvatarStorageKey = key;
        user.AvatarContentType = contentType;
        await db.SaveChangesAsync(ct);
        return previous;
    }
}
