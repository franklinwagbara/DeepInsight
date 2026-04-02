namespace DeepInsight.Infrastructure.Services;

public class GeoLookupService
{
    public GeoInfo Lookup(string? ipAddress)
    {
        // MVP: Return placeholder. In production, integrate MaxMind GeoIP2 or similar
        if (string.IsNullOrEmpty(ipAddress))
            return new GeoInfo { Country = "Unknown", City = "Unknown" };

        return new GeoInfo
        {
            Country = "Unknown",
            City = "Unknown"
        };
    }

    public static string AnonymizeIp(string? ip)
    {
        if (string.IsNullOrEmpty(ip)) return string.Empty;

        if (ip.Contains(':'))
        {
            var parts = ip.Split(':');
            if (parts.Length >= 4)
            {
                return string.Join(':', parts.Take(3)) + ":0:0:0:0:0";
            }
        }

        var ipv4Parts = ip.Split('.');
        if (ipv4Parts.Length == 4)
        {
            ipv4Parts[3] = "0";
            return string.Join('.', ipv4Parts);
        }

        return ip;
    }
}

public class GeoInfo
{
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}
