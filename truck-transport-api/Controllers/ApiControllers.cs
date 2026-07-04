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

    [HttpPost("forgot")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email is required." });
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.IsActive);
        await LoginLogService.LogAsync(db, HttpContext, normalizedEmail, "ForgotPassword", user is not null);

        // In this simplified flow we do NOT reveal existence — always return generic success
        return Ok(new { message = "If an account exists for that email, password reset instructions have been sent." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and password are required." });
        }

        if (request.Password != request.ConfirmPassword)
        {
            return BadRequest(new { message = "Passwords do not match." });
        }

        if (request.Password.Length < 6)
        {
            return BadRequest(new { message = "Password must be at least 6 characters." });
        }

        // No reset code required: allow password reset if email exists in DB
        var normalizedEmail = request.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return BadRequest(new { message = "Email is required." });
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.IsActive);
        if (user is null)
        {
            // Do not reveal too much; return generic not found
            return NotFound(new { message = "User not found." });
        }

        user.PasswordHash = PasswordHasher.Hash(request.Password);
        await db.SaveChangesAsync();
        await LoginLogService.LogAsync(db, HttpContext, user.Email, "ResetPassword", true, user.Id);

        return Ok(new { message = "Password reset successfully. Please sign in with your new password." });
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
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LookupItemDto>> CreateTruck([FromBody] CreateTruckRequest request)
    {
        var number = request.Number.Trim();
        if (string.IsNullOrWhiteSpace(number)) return BadRequest(new { message = "Truck number is required." });
        if (!ValidationHelper.IsValidIndianVehicle(number))
            return BadRequest(new { message = "Enter a valid Indian vehicle number (e.g. MH-12-AB-4521)." });
        if (await db.Trucks.AnyAsync(t => t.Number == number))
            return Conflict(new { message = "Truck already exists." });

        var truck = new Truck { Id = Guid.NewGuid(), Number = number };
        db.Trucks.Add(truck);
        await db.SaveChangesAsync();
        return Ok(new LookupItemDto(truck.Id, truck.Number, null, null));
    }

    [HttpPut("trucks/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LookupItemDto>> UpdateTruck(Guid id, [FromBody] CreateTruckRequest request)
    {
        var number = request.Number.Trim();
        if (string.IsNullOrWhiteSpace(number)) return BadRequest(new { message = "Truck number is required." });
        if (!ValidationHelper.IsValidIndianVehicle(number))
            return BadRequest(new { message = "Enter a valid Indian vehicle number (e.g. MH-12-AB-4521)." });

        if (await db.Trucks.AnyAsync(t => t.Number == number && t.Id != id && t.IsActive))
            return Conflict(new { message = "Another truck with this number already exists." });

        var truck = await db.Trucks.FindAsync(id);
        if (truck is null || !truck.IsActive) return NotFound();

        truck.Number = number;
        await db.SaveChangesAsync();
        return Ok(new LookupItemDto(truck.Id, truck.Number, null, null));
    }

    [HttpDelete("trucks/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTruck(Guid id)
    {
        var truck = await db.Trucks.FindAsync(id);
        if (truck is null || !truck.IsActive) return NotFound();
        truck.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("quarries")]
    public async Task<ActionResult<IEnumerable<LookupItemDto>>> GetQuarries() =>
        Ok(await db.Quarries.Where(q => q.IsActive).OrderBy(q => q.Name)
            .Select(q => new LookupItemDto(q.Id, q.Name, null, null)).ToListAsync());

    [HttpPost("quarries")]
    [Authorize(Roles = "Admin")]
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

    [HttpPut("quarries/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LookupItemDto>> UpdateQuarry(Guid id, [FromBody] CreateQuarryRequest request)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { message = "Quarry name is required." });

        if (await db.Quarries.AnyAsync(q => q.Name == name && q.Id != id && q.IsActive))
            return Conflict(new { message = "Another quarry with this name already exists." });

        var quarry = await db.Quarries.FindAsync(id);
        if (quarry is null || !quarry.IsActive) return NotFound();

        quarry.Name = name;
        await db.SaveChangesAsync();
        return Ok(new LookupItemDto(quarry.Id, quarry.Name, null, null));
    }

    [HttpDelete("quarries/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteQuarry(Guid id)
    {
        var quarry = await db.Quarries.FindAsync(id);
        if (quarry is null || !quarry.IsActive) return NotFound();
        quarry.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("drivers")]
    public async Task<ActionResult<IEnumerable<LookupItemDto>>> GetDrivers() =>
        Ok(await db.Drivers.Where(d => d.IsActive).OrderBy(d => d.Name)
            .Select(d => new LookupItemDto(d.Id, d.Name, d.Phone, d.License)).ToListAsync());

    [HttpPost("drivers")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LookupItemDto>> CreateDriver([FromBody] CreateDriverRequest request)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { message = "Driver name is required." });
        if (!ValidationHelper.IsValidIndianPhone(request.Phone))
            return BadRequest(new { message = "Enter a valid Indian mobile number (10 digits)." });
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

    [HttpPut("drivers/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LookupItemDto>> UpdateDriver(Guid id, [FromBody] CreateDriverRequest request)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { message = "Driver name is required." });
        if (!ValidationHelper.IsValidIndianPhone(request.Phone))
            return BadRequest(new { message = "Enter a valid Indian mobile number (10 digits)." });

        if (await db.Drivers.AnyAsync(d => d.Name == name && d.Id != id && d.IsActive))
            return Conflict(new { message = "Another driver with this name already exists." });

        var driver = await db.Drivers.FindAsync(id);
        if (driver is null || !driver.IsActive) return NotFound();

        driver.Name = name;
        driver.Phone = request.Phone.Trim();
        driver.License = request.License.Trim();
        await db.SaveChangesAsync();
        return Ok(new LookupItemDto(driver.Id, driver.Name, driver.Phone, driver.License));
    }

    [HttpDelete("drivers/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDriver(Guid id)
    {
        var driver = await db.Drivers.FindAsync(id);
        if (driver is null || !driver.IsActive) return NotFound();
        driver.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
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
        return Ok(new TripSummaryDto(
            trips.Sum(t => t.NumberOfTrips),
            trips.Sum(t => t.Tonnes),
            trips.Sum(t => t.DieselLiters),
            trips.Where(t => t.Shift == "Day").Sum(t => t.NumberOfTrips),
            trips.Where(t => t.Shift == "Night").Sum(t => t.NumberOfTrips)));
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
    public async Task<ActionResult<TripDto>> Create([FromBody] CreateTripRequest request)
    {
        var validationError = ValidateTripRequest(request.TruckNumber, request.DriverPhone);
        if (validationError is not null) return BadRequest(new { message = validationError });

        if (!await db.Trucks.AnyAsync(t => t.Number == request.TruckNumber && t.IsActive))
            return BadRequest(new { message = "Truck number must exist in master data. Please ask admin to add it." });
        if (!await db.Quarries.AnyAsync(q => q.Name == request.QuarryName && q.IsActive))
            return BadRequest(new { message = "Quarry / crusher must exist in master data. Please ask admin to add it." });
        if (!await db.Drivers.AnyAsync(d => d.Name == request.DriverName && d.IsActive))
            return BadRequest(new { message = "Driver must exist in master data. Please ask admin to add it." });
        if (!string.IsNullOrWhiteSpace(request.AdditionalDriverName) &&
            !await db.Drivers.AnyAsync(d => d.Name == request.AdditionalDriverName && d.IsActive))
            return BadRequest(new { message = "Additional driver must exist in master data. Please ask admin to add it." });

        var email = User.FindFirstValue(ClaimTypes.Email) ?? "unknown";

        var trip = new TransportTrip
        {
            Id = Guid.NewGuid(),
            Date = DateOnly.Parse(request.Date),
            Shift = request.Shift,
            TruckNumber = request.TruckNumber,
            QuarryName = request.QuarryName,
            NumberOfTrips = Math.Clamp(request.NumberOfTrips, 1, 50),
            Tonnes = Math.Max(0, request.Tonnes),
            DieselLiters = Math.Max(0, request.DieselLiters),
            DriverName = request.DriverName,
            AdditionalDriverName = request.AdditionalDriverName?.Trim(),
            DriverPhone = request.DriverPhone,
            DriverLicense = request.DriverLicense,
            StartTime = ValidationHelper.ParseOptionalTime(request.StartTime),
            EndTime = ValidationHelper.ParseOptionalTime(request.EndTime),
            CreatedBy = email,
        };
        db.Trips.Add(trip);
        await db.SaveChangesAsync();
        return Ok(TripQueryService.ToDto(trip));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TripDto>> Update(Guid id, [FromBody] UpdateTripRequest request)
    {
        var trip = await db.Trips.FindAsync(id);
        if (trip is null) return NotFound();

        var validationError = ValidateTripRequest(request.TruckNumber, request.DriverPhone);
        if (validationError is not null) return BadRequest(new { message = validationError });

        if (!await db.Trucks.AnyAsync(t => t.Number == request.TruckNumber && t.IsActive))
            return BadRequest(new { message = "Truck number must exist in master data. Please ask admin to add it." });
        if (!await db.Quarries.AnyAsync(q => q.Name == request.QuarryName && q.IsActive))
            return BadRequest(new { message = "Quarry / crusher must exist in master data. Please ask admin to add it." });
        if (!await db.Drivers.AnyAsync(d => d.Name == request.DriverName && d.IsActive))
            return BadRequest(new { message = "Driver must exist in master data. Please ask admin to add it." });
        if (!string.IsNullOrWhiteSpace(request.AdditionalDriverName) &&
            !await db.Drivers.AnyAsync(d => d.Name == request.AdditionalDriverName && d.IsActive))
            return BadRequest(new { message = "Additional driver must exist in master data. Please ask admin to add it." });
        trip.Date = DateOnly.Parse(request.Date);
        trip.Shift = request.Shift;
        trip.TruckNumber = request.TruckNumber;
        trip.QuarryName = request.QuarryName;
        trip.Tonnes = Math.Max(0, request.Tonnes);
        trip.DieselLiters = Math.Max(0, request.DieselLiters);
        trip.NumberOfTrips = Math.Clamp(request.NumberOfTrips, 1, 50);
        trip.DriverName = request.DriverName;
        trip.AdditionalDriverName = request.AdditionalDriverName?.Trim();
        trip.DriverPhone = request.DriverPhone;
        trip.DriverLicense = request.DriverLicense;
        trip.StartTime = ValidationHelper.ParseOptionalTime(request.StartTime);
        trip.EndTime = ValidationHelper.ParseOptionalTime(request.EndTime);
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

    private static string? ValidateTripRequest(string truckNumber, string driverPhone)
    {
        if (!ValidationHelper.IsValidIndianVehicle(truckNumber))
            return "Enter a valid Indian vehicle number (e.g. MH-12-AB-4521).";
        if (!ValidationHelper.IsValidIndianPhone(driverPhone))
            return "Enter a valid Indian mobile number (10 digits).";
        return null;
    }

}

[ApiController]
[Route("api/repairs")]
[Authorize]
public class RepairsController(AppDbContext db, RepairQueryService queryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RepairDto>>> GetRepairs([FromQuery] RepairFilterQuery filter)
    {
        var repairs = await queryService.ApplyFilters(filter)
            .OrderByDescending(r => r.Date)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync();
        return Ok(repairs.Select(RepairQueryService.ToDto));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<RepairSummaryDto>> GetSummary([FromQuery] RepairFilterQuery filter)
    {
        var repairs = await queryService.ApplyFilters(filter).ToListAsync();
        return Ok(new RepairSummaryDto(repairs.Count, repairs.Sum(r => r.Cost)));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RepairDto>> GetById(Guid id)
    {
        var repair = await db.Repairs.FindAsync(id);
        return repair is null ? NotFound() : Ok(RepairQueryService.ToDto(repair));
    }

    [HttpPost]
    public async Task<ActionResult<RepairDto>> Create([FromBody] CreateRepairRequest request)
    {
        var validationError = ValidateRepairRequest(request.TruckNumber, request.Description, request.Cost);
        if (validationError is not null) return BadRequest(new { message = validationError });

        if (!await db.Trucks.AnyAsync(t => t.Number == request.TruckNumber && t.IsActive))
            return BadRequest(new { message = "Truck number must exist in master data. Please ask admin to add it." });

        var email = User.FindFirstValue(ClaimTypes.Email) ?? "unknown";

        var repair = new TruckRepair
        {
            Id = Guid.NewGuid(),
            Date = DateOnly.Parse(request.Date),
            TruckNumber = request.TruckNumber.Trim(),
            Description = request.Description.Trim(),
            Cost = request.Cost,
            DriverName = string.IsNullOrWhiteSpace(request.DriverName) ? null : request.DriverName.Trim(),
            CreatedBy = email,
        };
        db.Repairs.Add(repair);
        await db.SaveChangesAsync();
        return Ok(RepairQueryService.ToDto(repair));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RepairDto>> Update(Guid id, [FromBody] UpdateRepairRequest request)
    {
        var repair = await db.Repairs.FindAsync(id);
        if (repair is null) return NotFound();

        var validationError = ValidateRepairRequest(request.TruckNumber, request.Description, request.Cost);
        if (validationError is not null) return BadRequest(new { message = validationError });

        if (!await db.Trucks.AnyAsync(t => t.Number == request.TruckNumber && t.IsActive))
            return BadRequest(new { message = "Truck number must exist in master data. Please ask admin to add it." });
        repair.Date = DateOnly.Parse(request.Date);
        repair.TruckNumber = request.TruckNumber.Trim();
        repair.Description = request.Description.Trim();
        repair.Cost = request.Cost;
        repair.DriverName = string.IsNullOrWhiteSpace(request.DriverName) ? null : request.DriverName.Trim();
        repair.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(RepairQueryService.ToDto(repair));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var repair = await db.Repairs.FindAsync(id);
        if (repair is null) return NotFound();
        db.Repairs.Remove(repair);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static string? ValidateRepairRequest(string truckNumber, string description, decimal cost)
    {
        if (!ValidationHelper.IsValidIndianVehicle(truckNumber))
            return "Enter a valid Indian vehicle number (e.g. MH-12-AB-4521).";
        if (string.IsNullOrWhiteSpace(description))
            return "Repair description is required.";
        if (cost < 0)
            return "Repair cost cannot be negative.";
        return null;
    }

    
}
