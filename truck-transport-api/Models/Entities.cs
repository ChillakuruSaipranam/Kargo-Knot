namespace TruckTransportApi.Models;

public class AppUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Truck
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class Quarry
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class Driver
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class TransportTrip
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public string Shift { get; set; } = "Day";
    public string TruckNumber { get; set; } = string.Empty;
    public string QuarryName { get; set; } = string.Empty;
    public decimal Tonnes { get; set; }
    public decimal DieselLiters { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public string? AdditionalDriverName { get; set; }
    public string DriverPhone { get; set; } = string.Empty;
    public string DriverLicense { get; set; } = string.Empty;
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int NumberOfTrips { get; set; } = 1;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class LoginLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class TruckRepair
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public string TruckNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public string? DriverName { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
