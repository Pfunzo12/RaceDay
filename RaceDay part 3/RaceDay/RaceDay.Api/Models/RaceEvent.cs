namespace RaceDay.Api.Models;

/// Table: Events
public class RaceEvent
{
    public int Id { get; set; }                       // event_id
    public int OrganiserId { get; set; }              // organiser_id
    public User? Organiser { get; set; }
    public string Name { get; set; } = "";            // name
    public string? Description { get; set; }          // description
    public DateTime EventDate { get; set; }           // event_date (date only)
    public string Location { get; set; } = "";        // location
    public string EventType { get; set; } = "";       // event_type
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;   // created_at

    // Added for Part 3
    public decimal DistanceKm { get; set; }           // distance_km
    public string? BannerImagePath { get; set; }      // banner_image_path

    public List<Category> Categories { get; set; } = new();
}
