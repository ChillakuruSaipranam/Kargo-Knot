using Microsoft.EntityFrameworkCore;
using TruckTransportApi.Models;

namespace TruckTransportApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Truck> Trucks => Set<Truck>();
    public DbSet<Quarry> Quarries => Set<Quarry>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<TransportTrip> Trips => Set<TransportTrip>();
    public DbSet<LoginLog> LoginLogs => Set<LoginLog>();
    public DbSet<TruckRepair> Repairs => Set<TruckRepair>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(e => e.HasIndex(x => x.Email).IsUnique());
        modelBuilder.Entity<Truck>(e => e.HasIndex(x => x.Number).IsUnique());
        modelBuilder.Entity<Quarry>(e => e.HasIndex(x => x.Name).IsUnique());
        modelBuilder.Entity<Driver>(e => e.HasIndex(x => x.Name).IsUnique());
        modelBuilder.Entity<TransportTrip>(e =>
        {
            e.Property(x => x.Tonnes).HasPrecision(10, 2);
            e.Property(x => x.DieselLiters).HasPrecision(10, 2);
        });
        modelBuilder.Entity<LoginLog>(e => e.HasIndex(x => x.CreatedAt));
        modelBuilder.Entity<TruckRepair>(e =>
        {
            e.Property(x => x.Cost).HasPrecision(10, 2);
            e.HasIndex(x => x.Date);
            e.HasIndex(x => x.TruckNumber);
        });
    }
}
