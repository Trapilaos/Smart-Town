using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace API
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

            builder.Entity<ParkingSpace>().HasData(new List<ParkingSpace>
            {
                new ParkingSpace { Id = 1, Name = "Parking A", MaxVehicles = 75, CurrentVehicles = 0 },
                new ParkingSpace { Id = 2, Name = "Parking B", MaxVehicles = 50, CurrentVehicles = 0 },
                new ParkingSpace { Id = 3, Name = "Parking C", MaxVehicles = 60, CurrentVehicles = 0 }
            });

            // Seed TrafficData using the generator
            var trafficData = TrafficDataGenerator.GenerateTrafficData();
            int id = -1;
            foreach (var data in trafficData)
            {

                data.Id = id--; // Ensure unique negative Id
            }
            builder.Entity<TrafficData>().HasData(trafficData);
        }
    }
}
