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
            query = query.Where(t => t.DriverName == filter.DriverName || t.AdditionalDriverName == filter.DriverName);
        if (filter.MinTonnes.HasValue)
            query = query.Where(t => t.Tonnes >= filter.MinTonnes.Value);
        if (filter.MaxTonnes.HasValue)
            query = query.Where(t => t.Tonnes <= filter.MaxTonnes.Value);

        return query;
    }

    public static TripDto ToDto(TransportTrip t) => new(
        t.Id, t.Date.ToString("yyyy-MM-dd"), t.Shift, t.TruckNumber, t.QuarryName,
        t.NumberOfTrips, t.Tonnes, t.DieselLiters, t.DriverName, t.DriverPhone, t.DriverLicense,
        t.AdditionalDriverName,
        t.StartTime?.ToString("HH:mm") ?? string.Empty,
        t.EndTime?.ToString("HH:mm") ?? string.Empty,
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
                g.Sum(t => t.NumberOfTrips),
                g.Sum(t => t.Tonnes),
                g.Where(t => t.Shift == "Day").Sum(t => t.NumberOfTrips),
                g.Where(t => t.Shift == "Night").Sum(t => t.NumberOfTrips)))
            .ToList();

        var groupBreakdown = new List<GroupBreakdownDto>();
        if (!string.IsNullOrWhiteSpace(filter.TruckNumber))
        {
            groupBreakdown = trips.GroupBy(t => t.QuarryName)
                .Select(g => new GroupBreakdownDto(g.Key, g.Sum(t => t.NumberOfTrips), g.Sum(t => t.Tonnes),
                    g.Where(t => t.Shift == "Day").Sum(t => t.NumberOfTrips),
                    g.Where(t => t.Shift == "Night").Sum(t => t.NumberOfTrips))).ToList();
        }
        else if (!string.IsNullOrWhiteSpace(filter.QuarryName))
        {
            groupBreakdown = trips.GroupBy(t => t.TruckNumber)
                .Select(g => new GroupBreakdownDto(g.Key, g.Sum(t => t.NumberOfTrips), g.Sum(t => t.Tonnes),
                    g.Where(t => t.Shift == "Day").Sum(t => t.NumberOfTrips),
                    g.Where(t => t.Shift == "Night").Sum(t => t.NumberOfTrips))).ToList();
        }
        else if (!string.IsNullOrWhiteSpace(filter.DriverName))
        {
            groupBreakdown = trips.GroupBy(t => t.TruckNumber)
                .Select(g => new GroupBreakdownDto(g.Key, g.Sum(t => t.NumberOfTrips), g.Sum(t => t.Tonnes),
                    g.Where(t => t.Shift == "Day").Sum(t => t.NumberOfTrips),
                    g.Where(t => t.Shift == "Night").Sum(t => t.NumberOfTrips))).ToList();
        }

        return new TripAnalyticsDto(
            trips.Sum(t => t.NumberOfTrips),
            trips.Sum(t => t.Tonnes),
            trips.Sum(t => t.DieselLiters),
            trips.Where(t => t.Shift == "Day").Sum(t => t.NumberOfTrips),
            trips.Where(t => t.Shift == "Night").Sum(t => t.NumberOfTrips),
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
        if (!string.IsNullOrWhiteSpace(filter.DriverName))
            query = query.Where(r => r.DriverName == filter.DriverName);
        if (filter.MinCost.HasValue)
            query = query.Where(r => r.Cost >= filter.MinCost.Value);
        if (filter.MaxCost.HasValue)
            query = query.Where(r => r.Cost <= filter.MaxCost.Value);

        return query;
    }

    public static RepairDto ToDto(TruckRepair r) => new(
        r.Id, r.Date.ToString("yyyy-MM-dd"), r.TruckNumber, r.Description, r.Cost,
        r.DriverName, r.CreatedBy, r.CreatedAt.ToString("o"));
}
