namespace RaceDay.Api.Models;

/// Table: Entries
public class EventEnrollment
{
    public int Id { get; set; }                       // entry_id
    public int UserId { get; set; }                   // participant_id
    public User? User { get; set; }
    public int CategoryId { get; set; }               // category_id
    public Category? Category { get; set; }
    public DateTime EnrolledOn { get; set; } = DateTime.UtcNow;   // entry_date
    public string Status { get; set; } = EnrolmentStatus.Pending; // status
}
