namespace RaceDay.Api.Models;

/// Table: Users
public class User
{
    public int Id { get; set; }                       // user_id
    public string FullName { get; set; } = "";        // full_name
    public string Email { get; set; } = "";           // email
    public string PasswordHash { get; set; } = "";    // password_hash
    public string Role { get; set; } = Roles.Participant;   // role
    public string? Phone { get; set; }                // phone
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;   // created_at

    // Added for Part 3 (profile page)
    public DateTime? DateOfBirth { get; set; }        // date_of_birth
    public string? EmergencyContact { get; set; }     // emergency_contact
    public string? ClubName { get; set; }             // club_name
    public string? ProfilePicturePath { get; set; }   // profile_picture_path
}
