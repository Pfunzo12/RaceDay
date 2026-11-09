namespace RaceDay.Api.Models;

/// Stored lowercase in the existing database (Entries.status).
public static class EnrolmentStatus
{
    public const string Pending = "pending";
    public const string Confirmed = "confirmed";
    public const string Cancelled = "cancelled";
    public static readonly string[] All = { Pending, Confirmed, Cancelled };
}
