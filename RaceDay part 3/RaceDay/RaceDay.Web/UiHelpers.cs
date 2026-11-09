namespace RaceDay.Web;

public static class UiHelpers
{
    public static string Ordinal(int n)
    {
        var lastTwo = n % 100;
        if (lastTwo is >= 11 and <= 13) return n + "th";
        return (n % 10) switch { 1 => n + "st", 2 => n + "nd", 3 => n + "rd", _ => n + "th" };
    }

    public static string FormatTime(TimeSpan t) => t.ToString(@"hh\:mm\:ss");

    public static string Capitalise(string? s) =>
        string.IsNullOrEmpty(s) ? "" : char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant();

    public static string Money(decimal price) => price == 0 ? "Free" : "R" + price.ToString("0.##");

    public static string CategoryDescription(decimal distanceKm, int? minAge, int? maxAge)
    {
        var parts = new List<string> { $"{distanceKm:0.##} km" };
        if (minAge.HasValue && maxAge.HasValue) parts.Add($"ages {minAge}-{maxAge}");
        else if (minAge.HasValue) parts.Add($"ages {minAge}+");
        else if (maxAge.HasValue) parts.Add($"up to age {maxAge}");
        return string.Join(" · ", parts);
    }
}
