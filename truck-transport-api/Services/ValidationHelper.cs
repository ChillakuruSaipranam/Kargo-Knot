using System.Text.RegularExpressions;

namespace TruckTransportApi.Services;

public static partial class ValidationHelper
{
    [GeneratedRegex(@"^(?:\+91[\s-]?)?[6-9](?:[\s-]?\d){9}$", RegexOptions.Compiled)]
    private static partial Regex IndianPhoneRegex();

    [GeneratedRegex(@"^[A-Z]{2}[\s-]?\d{1,2}[\s-]?[A-Z]{1,3}[\s-]?\d{4}$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex IndianVehicleRegex();

    public static bool IsValidIndianPhone(string? value) =>
        !string.IsNullOrWhiteSpace(value) && IndianPhoneRegex().IsMatch(value.Trim());

    public static bool IsValidIndianVehicle(string? value) =>
        !string.IsNullOrWhiteSpace(value) && IndianVehicleRegex().IsMatch(value.Trim());
}
