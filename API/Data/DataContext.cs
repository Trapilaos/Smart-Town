using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class DataContext : IdentityDbContext<AppUser, AppRole, int, IdentityUserClaim<int>, AppUserRole,
        IdentityUserLogin<int>, IdentityRoleClaim<int>, IdentityUserToken<int>>
    {
        public DataContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<ParkingSpace> ParkingSpaces { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<TrafficData> TrafficData { get; set; }
        public DbSet<WasteBin> WasteBins { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Municipality> Municipalities { get; set; }

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
                .WithOne(ur => ur.Role)
                .HasForeignKey(ur => ur.RoleId)
                .IsRequired();

            // Stored as text so the database reads as General, Recycling, ... rather than numbers.
            builder.Entity<WasteBin>()
                .Property(b => b.Type)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Entity<Municipality>(entity =>
            {
                entity.Property(m => m.Name).HasMaxLength(120).IsRequired();
                entity.Property(m => m.TimeZoneId).HasMaxLength(64).IsRequired();
                entity.Property(m => m.BoundarySource).HasMaxLength(200);
            });
        }
    }
}
