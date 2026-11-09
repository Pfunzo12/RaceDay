namespace RaceDay.Api.Models;

/// Table: Categories
public class Category
{
    public int Id { get; set; }                       // category_id
    public int EventId { get; set; }                  // event_id
    public RaceEvent? Event { get; set; }
    public string Name { get; set; } = "";            // name
    public decimal DistanceKm { get; set; }           // distance_km
    public decimal Price { get; set; }                // price
    public int MaxParticipants { get; set; } = 500;   // max_participants

    // Added for Part 3 (age categories)
    public int? MinAge { get; set; }                  // min_age
    public int? MaxAge { get; set; }                  // max_age
}
