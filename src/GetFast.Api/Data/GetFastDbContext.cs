using GetFast.Api.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GetFast.Api.Data;

public class GetFastDbContext(DbContextOptions<GetFastDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<IdentityRole<Guid>>().HasData(
            new IdentityRole<Guid> { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Name = RoleNames.Sender, NormalizedName = "SENDER", ConcurrencyStamp = "sender-v1" },
            new IdentityRole<Guid> { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Name = RoleNames.Courier, NormalizedName = "COURIER", ConcurrencyStamp = "courier-v1" },
            new IdentityRole<Guid> { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Name = RoleNames.Dispatcher, NormalizedName = "DISPATCHER", ConcurrencyStamp = "dispatcher-v1" });
    }
}
