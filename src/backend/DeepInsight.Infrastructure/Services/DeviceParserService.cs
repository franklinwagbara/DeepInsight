namespace DeepInsight.Infrastructure.Services;

public class DeviceParserService
{
    public DeviceInfo Parse(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return new DeviceInfo { DeviceType = "unknown", Browser = "unknown", Os = "unknown" };

        return new DeviceInfo
        {
            DeviceType = DetectDeviceType(userAgent),
            Browser = DetectBrowser(userAgent),
            Os = DetectOs(userAgent)
        };
    }

    private static string DetectDeviceType(string ua)
    {
        if (ua.Contains("Mobile", StringComparison.OrdinalIgnoreCase) ||
            ua.Contains("Android", StringComparison.OrdinalIgnoreCase) && !ua.Contains("Tablet", StringComparison.OrdinalIgnoreCase))
            return "mobile";

        if (ua.Contains("Tablet", StringComparison.OrdinalIgnoreCase) ||
            ua.Contains("iPad", StringComparison.OrdinalIgnoreCase))
            return "tablet";

        return "desktop";
    }

    private static string DetectBrowser(string ua)
    {
        if (ua.Contains("Edg/", StringComparison.OrdinalIgnoreCase)) return "Edge";
        if (ua.Contains("Chrome/", StringComparison.OrdinalIgnoreCase) &&
            !ua.Contains("Edg/", StringComparison.OrdinalIgnoreCase)) return "Chrome";
        if (ua.Contains("Firefox/", StringComparison.OrdinalIgnoreCase)) return "Firefox";
        if (ua.Contains("Safari/", StringComparison.OrdinalIgnoreCase) &&
            !ua.Contains("Chrome/", StringComparison.OrdinalIgnoreCase)) return "Safari";
        if (ua.Contains("Opera", StringComparison.OrdinalIgnoreCase) ||
            ua.Contains("OPR/", StringComparison.OrdinalIgnoreCase)) return "Opera";
        return "Other";
    }

    private static string DetectOs(string ua)
    {
        if (ua.Contains("Windows NT", StringComparison.OrdinalIgnoreCase)) return "Windows";
        if (ua.Contains("Mac OS X", StringComparison.OrdinalIgnoreCase)) return "macOS";
        if (ua.Contains("Linux", StringComparison.OrdinalIgnoreCase) &&
            !ua.Contains("Android", StringComparison.OrdinalIgnoreCase)) return "Linux";
        if (ua.Contains("Android", StringComparison.OrdinalIgnoreCase)) return "Android";
        if (ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
            ua.Contains("iPad", StringComparison.OrdinalIgnoreCase)) return "iOS";
        return "Other";
    }
}

public class DeviceInfo
{
    public string DeviceType { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string Os { get; set; } = string.Empty;
}
