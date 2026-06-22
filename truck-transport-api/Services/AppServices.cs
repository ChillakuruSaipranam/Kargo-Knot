using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TruckTransportApi.Data;
using TruckTransportApi.DTOs;
using TruckTransportApi.Models;

namespace TruckTransportApi.Services;

public static class PasswordHasher
{
    public static string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public static bool Verify(string password, string hash) => Hash(password) == hash;
}

public class TripQueryService(AppDbContext db)
{
    public IQueryable<TransportTrip> ApplyFilters(TripFilterQuery filter)
    {
        var query = db.Trips.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.DateFrom) && DateOnly.TryParse(filter.DateFrom, out var from))
            query = query.Where(t => t.Date >= from);
        if (!string.IsNullOrWhiteSpace(filter.DateTo) && DateOnly.TryParse(filter.DateTo, out var to))
            query = query.Where(t => t.Date <= to);
        if (!string.IsNullOrWhiteSpace(filter.Shift))
            query = query.Where(t => t.Shift == filter.Shift);
        if (!string.IsNullOrWhiteSpace(filter.TruckNumber))
            query = query.Where(t => t.TruckNumber == filter.TruckNumber);
        if (!string.IsNullOrWhiteSpace(filter.QuarryName))
            query = query.Where(t => t.QuarryName == filter.QuarryName);
        if (!string.IsNullOrWhiteSpace(filter.DriverName))
            query = query.Where(t => t.DriverName == filter.DriverName);
        if (filter.MinTonnes.HasValue)
            query = query.Where(t => t.Tonnes >= filter.MinTonnes.Value);
        if (filter.MaxTonnes.HasValue)
            query = query.Where(t => t.Tonnes <= filter.MaxTonnes.Value);

        return query;
    }

    public static TripDto ToDto(TransportTrip t) => new(
        t.Id, t.Date.ToString("yyyy-MM-dd"), t.Shift, t.TruckNumber, t.QuarryName,
        t.NumberOfTrips, t.Tonnes, t.DieselLiters, t.DriverName, t.DriverPhone, t.DriverLicense,
        t.StartTime.ToString("HH:mm"), t.EndTime.ToString("HH:mm"),
        t.CreatedBy, t.CreatedAt.ToString("o"));

    public TripAnalyticsDto BuildAnalytics(List<TransportTrip> trips, TripFilterQuery filter)
    {
        var activeFilters = new List<string>();
        if (!string.IsNullOrWhiteSpace(filter.DateFrom)) activeFilters.Add($"From: {filter.DateFrom}");
        if (!string.IsNullOrWhiteSpace(filter.DateTo)) activeFilters.Add($"To: {filter.DateTo}");
        if (!string.IsNullOrWhiteSpace(filter.Shift)) activeFilters.Add($"Shift: {filter.Shift}");
        if (!string.IsNullOrWhiteSpace(filter.TruckNumber)) activeFilters.Add($"Truck: {filter.TruckNumber}");
        if (!string.IsNullOrWhiteSpace(filter.QuarryName)) activeFilters.Add($"Quarry: {filter.QuarryName}");
        if (!string.IsNullOrWhiteSpace(filter.DriverName)) activeFilters.Add($"Driver: {filter.DriverName}");
        if (filter.MinTonnes.HasValue) activeFilters.Add($"Min Tonnes: {filter.MinTonnes}");
        if (filter.MaxTonnes.HasValue) activeFilters.Add($"Max Tonnes: {filter.MaxTonnes}");

        var breakdownType = "Date";
        if (!string.IsNullOrWhiteSpace(filter.TruckNumber)) breakdownType = "Date";
        else if (!string.IsNullOrWhiteSpace(filter.QuarryName)) breakdownType = "Date";
        else if (!string.IsNullOrWhiteSpace(filter.DriverName)) breakdownType = "Date";

        var dateBreakdown = trips
            .GroupBy(t => t.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DateBreakdownDto(
                g.Key.ToString("yyyy-MM-dd"),
                g.Count(),
                g.Sum(t => t.Tonnes),
                g.Count(t => t.Shift == "Day"),
                g.Count(t => t.Shift == "Night")))
            .ToList();

        var groupBreakdown = new List<GroupBreakdownDto>();
        if (!string.IsNullOrWhiteSpace(filter.TruckNumber))
        {
            groupBreakdown = trips.GroupBy(t => t.QuarryName)
                .Select(g => new GroupBreakdownDto(g.Key, g.Count(), g.Sum(t => t.Tonnes),
                    g.Count(t => t.Shift == "Day"), g.Count(t => t.Shift == "Night"))).ToList();
        }
        else if (!string.IsNullOrWhiteSpace(filter.QuarryName))
        {
            groupBreakdown = trips.GroupBy(t => t.TruckNumber)
                .Select(g => new GroupBreakdownDto(g.Key, g.Count(), g.Sum(t => t.Tonnes),
                    g.Count(t => t.Shift == "Day"), g.Count(t => t.Shift == "Night"))).ToList();
        }
        else if (!string.IsNullOrWhiteSpace(filter.DriverName))
        {
            groupBreakdown = trips.GroupBy(t => t.TruckNumber)
                .Select(g => new GroupBreakdownDto(g.Key, g.Count(), g.Sum(t => t.Tonnes),
                    g.Count(t => t.Shift == "Day"), g.Count(t => t.Shift == "Night"))).ToList();
        }

        return new TripAnalyticsDto(
            trips.Count,
            trips.Sum(t => t.Tonnes),
            trips.Sum(t => t.DieselLiters),
            trips.Count(t => t.Shift == "Day"),
            trips.Count(t => t.Shift == "Night"),
            activeFilters,
            dateBreakdown,
            groupBreakdown,
            breakdownType);
    }
}

public static class LoginLogService
{
    public static async Task LogAsync(AppDbContext db, HttpContext http, string email, string action, bool success, Guid? userId = null)
    {
        db.LoginLogs.Add(new LoginLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = email,
            Action = action,
            Success = success,
            IpAddress = http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            UserAgent = http.Request.Headers.UserAgent.ToString(),
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }
}

public class JwtTokenService(IConfiguration config)
{
    public string CreateToken(AppUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role),
        };
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Users.AnyAsync())
        {
            db.Users.AddRange(
                new AppUser { Id = Guid.NewGuid(), Email = "admin@transport.com", PasswordHash = PasswordHasher.Hash("admin123"), FullName = "System Administrator", Role = "Admin" },
                new AppUser { Id = Guid.NewGuid(), Email = "user@transport.com", PasswordHash = PasswordHasher.Hash("user123"), FullName = "Operations User", Role = "User" });
        }

        if (!await db.Trucks.AnyAsync())
        {
            db.Trucks.AddRange(
                new Truck { Id = Guid.NewGuid(), Number = "MH-12-AB-4521" },
                new Truck { Id = Guid.NewGuid(), Number = "MH-12-CD-7788" },
                new Truck { Id = Guid.NewGuid(), Number = "MH-14-EF-3301" });
        }

        if (!await db.Quarries.AnyAsync())
        {
            db.Quarries.AddRange(
                new Quarry { Id = Guid.NewGuid(), Name = "Shivam Stone Crusher #3" },
                new Quarry { Id = Guid.NewGuid(), Name = "Blue Ridge Quarry" },
                new Quarry { Id = Guid.NewGuid(), Name = "Granite Hills Crusher #1" });
        }

        if (!await db.Drivers.AnyAsync())
        {
            db.Drivers.AddRange(
                new Driver { Id = Guid.NewGuid(), Name = "Rajesh Kumar", Phone = "+91 98765 43210", License = "MH-2024-88912" },
                new Driver { Id = Guid.NewGuid(), Name = "Suresh Patil", Phone = "+91 91234 56789", License = "MH-2023-44102" },
                new Driver { Id = Guid.NewGuid(), Name = "Amit Deshmukh", Phone = "+91 99887 76655", License = "MH-2025-10234" });
        }

        if (!await db.Trips.AnyAsync())
        {
            db.Trips.AddRange(
                new TransportTrip { Id = Guid.NewGuid(), Date = new DateOnly(2026, 6, 12), Shift = "Day", TruckNumber = "MH-12-AB-4521", QuarryName = "Shivam Stone Crusher #3", Tonnes = 18.5m, DieselLiters = 45m, DriverName = "Rajesh Kumar", DriverPhone = "+91 98765 43210", DriverLicense = "MH-2024-88912", StartTime = new TimeOnly(6, 30), EndTime = new TimeOnly(14, 15), NumberOfTrips = 1, CreatedBy = "admin@transport.com" },
                new TransportTrip { Id = Guid.NewGuid(), Date = new DateOnly(2026, 6, 12), Shift = "Night", TruckNumber = "MH-12-CD-7788", QuarryName = "Blue Ridge Quarry", Tonnes = 22m, DieselLiters = 52m, DriverName = "Suresh Patil", DriverPhone = "+91 91234 56789", DriverLicense = "MH-2023-44102", StartTime = new TimeOnly(20, 0), EndTime = new TimeOnly(4, 30), NumberOfTrips = 1, CreatedBy = "user@transport.com" },
                new TransportTrip { Id = Guid.NewGuid(), Date = new DateOnly(2026, 6, 13), Shift = "Day", TruckNumber = "MH-14-EF-3301", QuarryName = "Granite Hills Crusher #1", Tonnes = 15.75m, DieselLiters = 38m, DriverName = "Amit Deshmukh", DriverPhone = "+91 99887 76655", DriverLicense = "MH-2025-10234", StartTime = new TimeOnly(7, 0), EndTime = new TimeOnly(15, 45), NumberOfTrips = 1, CreatedBy = "user@transport.com" },
                new TransportTrip { Id = Guid.NewGuid(), Date = new DateOnly(2026, 6, 13), Shift = "Day", TruckNumber = "MH-12-AB-4521", QuarryName = "Shivam Stone Crusher #3", Tonnes = 20m, DieselLiters = 48m, DriverName = "Rajesh Kumar", DriverPhone = "+91 98765 43210", DriverLicense = "MH-2024-88912", StartTime = new TimeOnly(7, 0), EndTime = new TimeOnly(15, 0), NumberOfTrips = 1, CreatedBy = "admin@transport.com" },
                new TransportTrip { Id = Guid.NewGuid(), Date = new DateOnly(2026, 6, 14), Shift = "Night", TruckNumber = "MH-12-AB-4521", QuarryName = "Blue Ridge Quarry", Tonnes = 19.25m, DieselLiters = 50m, DriverName = "Suresh Patil", DriverPhone = "+91 91234 56789", DriverLicense = "MH-2023-44102", StartTime = new TimeOnly(21, 0), EndTime = new TimeOnly(5, 0), NumberOfTrips = 1, CreatedBy = "user@transport.com" });
        }

        if (!await db.Repairs.AnyAsync())
        {
            db.Repairs.AddRange(
                new TruckRepair { Id = Guid.NewGuid(), Date = new DateOnly(2026, 6, 10), TruckNumber = "MH-12-AB-4521", Description = "Brake pad replacement and wheel alignment", Cost = 12500m, CreatedBy = "admin@transport.com" },
                new TruckRepair { Id = Guid.NewGuid(), Date = new DateOnly(2026, 6, 11), TruckNumber = "MH-12-CD-7788", Description = "Engine oil change and air filter service", Cost = 4800m, CreatedBy = "user@transport.com" },
                new TruckRepair { Id = Guid.NewGuid(), Date = new DateOnly(2026, 6, 13), TruckNumber = "MH-14-EF-3301", Description = "Tyre replacement (2 rear tyres)", Cost = 28000m, CreatedBy = "admin@transport.com" });
        }

        await db.SaveChangesAsync();
    }
}

public class RepairQueryService(AppDbContext db)
{
    public IQueryable<TruckRepair> ApplyFilters(RepairFilterQuery filter)
    {
        var query = db.Repairs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.DateFrom) && DateOnly.TryParse(filter.DateFrom, out var from))
            query = query.Where(r => r.Date >= from);
        if (!string.IsNullOrWhiteSpace(filter.DateTo) && DateOnly.TryParse(filter.DateTo, out var to))
            query = query.Where(r => r.Date <= to);
        if (!string.IsNullOrWhiteSpace(filter.TruckNumber))
            query = query.Where(r => r.TruckNumber == filter.TruckNumber);
        if (filter.MinCost.HasValue)
            query = query.Where(r => r.Cost >= filter.MinCost.Value);
        if (filter.MaxCost.HasValue)
            query = query.Where(r => r.Cost <= filter.MaxCost.Value);

        return query;
    }

    public static RepairDto ToDto(TruckRepair r) => new(
        r.Id, r.Date.ToString("yyyy-MM-dd"), r.TruckNumber, r.Description, r.Cost,
        r.CreatedBy, r.CreatedAt.ToString("o"));
}
