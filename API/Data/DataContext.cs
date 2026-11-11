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
        public DbSet<EventInterest> EventInterests { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Municipality> Municipalities { get; set; }
        public DbSet<AuditEntry> AuditEntries { get; set; }

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

            builder.Entity<ParkingSpace>(entity =>
            {
                entity.Property(p => p.Name).HasMaxLength(120);
                entity.Property(p => p.Address).HasMaxLength(200);
                entity.Property(p => p.Type).HasConversion<string>().HasMaxLength(20);
            });

            builder.Entity<Event>(entity =>
            {
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.Location).HasMaxLength(200);
                entity.Property(e => e.Category).HasConversion<string>().HasMaxLength(20);
            });

            builder.Entity<EventInterest>(entity =>
            {
                entity.HasKey(i => new { i.EventId, i.UserId });
                entity.Property(i => i.UserId).HasMaxLength(64).IsRequired();
                entity.HasOne(i => i.Event)
                    .WithMany()
                    .HasForeignKey(i => i.EventId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Invoice>(entity =>
            {
                entity.Property(i => i.InvoiceNumber).HasMaxLength(32).IsRequired();
                entity.HasIndex(i => i.InvoiceNumber).IsUnique();
                entity.Property(i => i.PaymentCode).HasMaxLength(8);
                entity.Property(i => i.ReceiptReference).HasMaxLength(64);
            });

            builder.Entity<AuditEntry>(entity =>
            {
                entity.Property(a => a.Action).HasMaxLength(60).IsRequired();
                entity.Property(a => a.EntityType).HasMaxLength(60).IsRequired();
                entity.Property(a => a.EntityId).HasMaxLength(60);
                entity.Property(a => a.Username).HasMaxLength(120);
                entity.Property(a => a.Details).HasMaxLength(500);
                entity.HasIndex(a => a.AtUtc);
            });
        }
    }
}
