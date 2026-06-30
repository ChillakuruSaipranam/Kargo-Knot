namespace TruckTransportApi.DTOs;

public record LoginRequest(string Email, string Password);
public record SignupRequest(string Email, string Password, string FullName);
public record UserDto(Guid Id, string Email, string FullName, string Role);
public record LoginResponse(string Token, UserDto User);

public record TripDto(
    Guid Id, string Date, string Shift, string TruckNumber, string QuarryName,
    int NumberOfTrips, decimal Tonnes, decimal DieselLiters, string DriverName, string DriverPhone, string DriverLicense,
    string StartTime, string EndTime, string CreatedBy, string CreatedAt);

public record CreateTripRequest(
    string Date, string Shift, string TruckNumber, string QuarryName, decimal Tonnes,
    decimal DieselLiters, string DriverName, string DriverPhone, string DriverLicense,
    string StartTime, string EndTime, int NumberOfTrips = 1);

public record UpdateTripRequest(
    string Date, string Shift, string TruckNumber, string QuarryName, decimal Tonnes,
    decimal DieselLiters, string DriverName, string DriverPhone, string DriverLicense,
    string StartTime, string EndTime, int NumberOfTrips = 1);

public record CreateTruckRequest(string Number);
public record CreateQuarryRequest(string Name);
public record CreateDriverRequest(string Name, string Phone, string License);

public record LookupItemDto(Guid Id, string Label, string? Phone, string? License);
public record DateBreakdownDto(string Date, int TripCount, decimal TotalTonnes, int DayTrips, int NightTrips);
public record GroupBreakdownDto(string GroupKey, int TripCount, decimal TotalTonnes, int DayTrips, int NightTrips);

public record TripAnalyticsDto(
    int TotalTrips, decimal TotalTonnes, decimal TotalDieselLiters, int DayTrips, int NightTrips,
    IReadOnlyList<string> ActiveFilters, IReadOnlyList<DateBreakdownDto> DateBreakdown,
    IReadOnlyList<GroupBreakdownDto> GroupBreakdown, string BreakdownType);

public record TripSummaryDto(int TotalTrips, decimal TotalTonnes, decimal TotalDieselLiters, int DayTrips, int NightTrips);

public record TripFilterQuery(
    string? DateFrom, string? DateTo, string? Shift,
    string? TruckNumber, string? QuarryName, string? DriverName,
    decimal? MinTonnes, decimal? MaxTonnes);

public record LoginLogDto(
    Guid Id, Guid? UserId, string Email, string Action, bool Success,
    string IpAddress, string UserAgent, string CreatedAt);

public record RepairDto(
    Guid Id, string Date, string TruckNumber, string Description, decimal Cost,
    string CreatedBy, string CreatedAt);

public record CreateRepairRequest(string Date, string TruckNumber, string Description, decimal Cost);

public record UpdateRepairRequest(string Date, string TruckNumber, string Description, decimal Cost);

public record RepairSummaryDto(int TotalRepairs, decimal TotalCost);

public record RepairFilterQuery(
    string? DateFrom, string? DateTo, string? TruckNumber,
    decimal? MinCost, decimal? MaxCost);
