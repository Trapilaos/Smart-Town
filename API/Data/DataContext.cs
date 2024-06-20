using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace API;

public class DataContext : IdentityDbContext<AppUser, AppRole, int, IdentityUserClaim<int>, AppUserRole,
 IdentityUserLogin<int>, IdentityRoleClaim<int>, IdentityUserToken<int>>
{
    public DataContext(DbContextOptions options) : base(options)
    {
    }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<Photo> Photos { get; set; }
    protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AppUser>()
                .HasMany(ur => ur.UserRoles)
                .WithOne(u => u.User)
                .HasForeignKey(ur => ur.UserId)
                .IsRequired();

            builder.Entity<AppRole>()
                .HasMany(ur => ur.UserRoles)
                .WithOne(u => u.Role)
                .HasForeignKey(ur => ur.RoleId)
                .IsRequired();

            builder.Entity<Invoice>().HasData(new List<Invoice>
            {
                new Invoice { Id = 1, InvoiceNumber = "INV-001", Amount = 100.00M, UserId = "1", IsPaid = false },
                new Invoice { Id = 2, InvoiceNumber = "INV-002", Amount = 150.50M, UserId = "2", IsPaid = false },
                new Invoice { Id = 3, InvoiceNumber = "INV-003", Amount = 200.75M, UserId = "3", IsPaid = false },
                new Invoice { Id = 4, InvoiceNumber = "INV-004", Amount = 120.00M, UserId = "4", IsPaid = false },
                new Invoice { Id = 5, InvoiceNumber = "INV-005", Amount = 300.20M, UserId = "5", IsPaid = false },
                new Invoice { Id = 6, InvoiceNumber = "INV-006", Amount = 80.00M, UserId = "6", IsPaid = false }
            });
        }
}
