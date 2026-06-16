using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckTransportApi.Data;
using TruckTransportApi.DTOs;
using TruckTransportApi.Models;
using TruckTransportApi.Services;

namespace TruckTransportApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AppDbContext db, JwtTokenService jwt) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            await LoginLogService.LogAsync(db, HttpContext, request.Email, "Login", false);
            return Unauthorized(new { message = "Invalid credentials." });
        }

        await LoginLogService.LogAsync(db, HttpContext, user.Email, "Login", true, user.Id);
        var token = jwt.CreateToken(user);
        return Ok(new LoginResponse(token, new UserDto(user.Id, user.Email, user.FullName, user.Role)));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] SignupRequest request)
    {
        if (await db.Users.AnyAsync(u => u.Email == request.Email))
        {
            await LoginLogService.LogAsync(db, HttpContext, request.Email, "Signup", false);
            return Conflict(new { message = "An account with this email already exists." });
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = PasswordHasher.Hash(request.Password),
            FullName = request.FullName.Trim(),
            Role = "User",
            IsActive = true,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        await LoginLogService.LogAsync(db, HttpContext, user.Email, "Signup", true, user.Id);

        var token = jwt.CreateToken(user);
        return Ok(new LoginResponse(token, new UserDto(user.Id, user.Email, user.FullName, user.Role)));
    }

    [HttpGet("logs")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<LoginLogDto>>> GetLogs([FromQuery] int limit = 100)
    {
        var logs = await db.LoginLogs
            .OrderByDescending(l => l.CreatedAt)
            .Take(Math.Clamp(limit, 1, 500))
            .Select(l => new LoginLogDto(l.Id, l.UserId, l.Email, l.Action, l.Success, l.IpAddress, l.UserAgent, l.CreatedAt.ToString("o")))
            .ToListAsync();
        return Ok(logs);
    }
}

[ApiController]
[Route("api/lookups")]
[Authorize]
public class LookupsController(AppDbContext db) : ControllerBase
{
    [HttpGet("trucks")]
    public async Task<ActionResult<IEnumerable<LookupItemDto>>> GetTrucks() =>
        Ok(await db.Trucks.Where(t => t.IsActive).OrderBy(t => t.Number)
            .Select(t => new LookupItemDto(t.Id, t.Number, null, null)).ToListAsync());

    [HttpPost("trucks")]
    public async Task<ActionResult<LookupItemDto>> CreateTruck([FromBody] CreateTruckRequest request)
    {
        var number = request.Number.Trim();
        if (string.IsNullOrWhiteSpace(number)) return BadRequest(new { message = "Truck number is required." });
        if (await db.Trucks.AnyAsync(t => t.Number == number))
            return Conflict(new { message = "Truck already exists." });

        var truck = new Truck { Id = Guid.NewGuid(), Number = number };
        db.Trucks.Add(truck);
        await db.SaveChangesAsync();
        return Ok(new LookupItemDto(truck.Id, truck.Number, null, null));
    }

    [HttpGet("quarries")]
    public async Task<ActionResult<IEnumerable<LookupItemDto>>> GetQuarries() =>
        Ok(await db.Quarries.Where(q => q.IsActive).OrderBy(q => q.Name)
            .Select(q => new LookupItemDto(q.Id, q.Name, null, null)).ToListAsync());

    [HttpPost("quarries")]
    public async Task<ActionResult<LookupItemDto>> CreateQuarry([FromBody] CreateQuarryRequest request)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { message = "Quarry name is required." });
        if (await db.Quarries.AnyAsync(q => q.Name == name))
            return Conflict(new { message = "Quarry already exists." });

        var quarry = new Quarry { Id = Guid.NewGuid(), Name = name };
        db.Quarries.Add(quarry);
        await db.SaveChangesAsync();
        return Ok(new LookupItemDto(quarry.Id, quarry.Name, null, null));
    }

    [HttpGet("drivers")]
    public async Task<ActionResult<IEnumerable<LookupItemDto>>> GetDrivers() =>
        Ok(await db.Drivers.Where(d => d.IsActive).OrderBy(d => d.Name)
            .Select(d => new LookupItemDto(d.Id, d.Name, d.Phone, d.License)).ToListAsync());

    [HttpPost("drivers")]
    public async Task<ActionResult<LookupItemDto>> CreateDriver([FromBody] CreateDriverRequest request)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { message = "Driver name is required." });
        if (await db.Drivers.AnyAsync(d => d.Name == name))
            return Conflict(new { message = "Driver already exists." });

        var driver = new Driver
        {
            Id = Guid.NewGuid(),
            Name = name,
            Phone = request.Phone.Trim(),
            License = request.License.Trim(),
        };
        db.Drivers.Add(driver);
        await db.SaveChangesAsync();
        return Ok(new LookupItemDto(driver.Id, driver.Name, driver.Phone, driver.License));
    }
}

[ApiController]
[Route("api/trips")]
[Authorize]
public class TripsController(AppDbContext db, TripQueryService queryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TripDto>>> GetTrips([FromQuery] TripFilterQuery filter)
    {
        var trips = await queryService.ApplyFilters(filter).OrderByDescending(t => t.Date).ThenByDescending(t => t.CreatedAt).ToListAsync();
        return Ok(trips.Select(TripQueryService.ToDto));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<TripSummaryDto>> GetSummary()
    {
        var trips = await db.Trips.ToListAsync();
        return Ok(new TripSummaryDto(trips.Count, trips.Sum(t => t.Tonnes), trips.Sum(t => t.DieselLiters), trips.Count(t => t.Shift == "Day"), trips.Count(t => t.Shift == "Night")));
    }

    [HttpGet("analytics")]
    public async Task<ActionResult<TripAnalyticsDto>> GetAnalytics([FromQuery] TripFilterQuery filter)
    {
        var trips = await queryService.ApplyFilters(filter).ToListAsync();
        return Ok(queryService.BuildAnalytics(trips, filter));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TripDto>> GetById(Guid id)
    {
        var trip = await db.Trips.FindAsync(id);
        return trip is null ? NotFound() : Ok(TripQueryService.ToDto(trip));
    }

    [HttpPost]
    public async Task<ActionResult<IEnumerable<TripDto>>> Create([FromBody] CreateTripRequest request)
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? "unknown";
        var count = Math.Clamp(request.NumberOfTrips, 1, 50);
        await EnsureLookupsExist(request.TruckNumber, request.QuarryName, request.DriverName, request.DriverPhone, request.DriverLicense);

        var created = new List<TransportTrip>();
        for (var i = 0; i < count; i++)
        {
            var trip = new TransportTrip
            {
                Id = Guid.NewGuid(),
                Date = DateOnly.Parse(request.Date),
                Shift = request.Shift,
                TruckNumber = request.TruckNumber,
                QuarryName = request.QuarryName,
                    NumberOfTrips = request.NumberOfTrips,
                    Tonnes = request.Tonnes,
                    DieselLiters = request.DieselLiters,
                DriverName = request.DriverName,
                DriverPhone = request.DriverPhone,
                DriverLicense = request.DriverLicense,
                StartTime = TimeOnly.Parse(request.StartTime),
                EndTime = TimeOnly.Parse(request.EndTime),
                CreatedBy = email,
            };
            db.Trips.Add(trip);
            created.Add(trip);
        }
        await db.SaveChangesAsync();
        return Ok(created.Select(TripQueryService.ToDto));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TripDto>> Update(Guid id, [FromBody] UpdateTripRequest request)
    {
        var trip = await db.Trips.FindAsync(id);
        if (trip is null) return NotFound();

        await EnsureLookupsExist(request.TruckNumber, request.QuarryName, request.DriverName, request.DriverPhone, request.DriverLicense);
        trip.Date = DateOnly.Parse(request.Date);
        trip.Shift = request.Shift;
        trip.TruckNumber = request.TruckNumber;
        trip.QuarryName = request.QuarryName;
        trip.Tonnes = request.Tonnes;
        trip.DieselLiters = request.DieselLiters;
        trip.DriverName = request.DriverName;
        trip.DriverPhone = request.DriverPhone;
        trip.DriverLicense = request.DriverLicense;
        trip.StartTime = TimeOnly.Parse(request.StartTime);
        trip.EndTime = TimeOnly.Parse(request.EndTime);
        trip.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(TripQueryService.ToDto(trip));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var trip = await db.Trips.FindAsync(id);
        if (trip is null) return NotFound();
        db.Trips.Remove(trip);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task EnsureLookupsExist(string truckNumber, string quarryName, string driverName, string driverPhone, string driverLicense)
    {
        if (!await db.Trucks.AnyAsync(t => t.Number == truckNumber))
            db.Trucks.Add(new Truck { Id = Guid.NewGuid(), Number = truckNumber });
        if (!await db.Quarries.AnyAsync(q => q.Name == quarryName))
            db.Quarries.Add(new Quarry { Id = Guid.NewGuid(), Name = quarryName });
        if (!await db.Drivers.AnyAsync(d => d.Name == driverName))
            db.Drivers.Add(new Driver { Id = Guid.NewGuid(), Name = driverName, Phone = driverPhone, License = driverLicense });
        await db.SaveChangesAsync();
    }
}
