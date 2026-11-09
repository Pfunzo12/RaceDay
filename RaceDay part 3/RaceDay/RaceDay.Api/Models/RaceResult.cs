namespace RaceDay.Api.Models;

/// Table: Results
public class RaceResult
{
    public int Id { get; set; }                       // result_id
    public int EnrollmentId { get; set; }             // entry_id
    public EventEnrollment? Enrollment { get; set; }
    public TimeSpan? FinishTime { get; set; }         // finish_time
    public int? Position { get; set; }                // position_overall
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;  // recorded_at

    // Added for Part 3: participants only see published results
    public bool IsPublished { get; set; }             // is_published
}
