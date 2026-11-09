using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public static class EventTypes
{
    // Default choices; the form also offers any event types already stored in the database.
    public static readonly string[] All = { "Road Run", "Trail Run", "Fun Run", "Walk", "Cycling", "Triathlon" };
}

public static class AppRoles
{
    public const string Organiser = "organiser";
    public const string Participant = "participant";
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email."), EmailAddress(ErrorMessage = "That doesn't look like a valid email.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please enter your password."), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your full name."), StringLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Please enter your email."), EmailAddress(ErrorMessage = "That doesn't look like a valid email."), StringLength(255)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please choose a password."), StringLength(100, MinimumLength = 6, ErrorMessage = "Use at least 6 characters."), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Please confirm your password."), DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "The passwords do not match."), Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";

    [Required(ErrorMessage = "Please choose how you will use RaceDay.")]
    [Display(Name = "I am joining as")]
    public string Role { get; set; } = AppRoles.Participant;
}

public class EventFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Event name is required."), StringLength(150)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Please describe the event."), StringLength(2000)]
    public string Description { get; set; } = "";

    [Required(ErrorMessage = "Please choose the event date."), DataType(DataType.Date),
     DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true), Display(Name = "Event date")]
    public DateTime EventDate { get; set; }

    [Required(ErrorMessage = "Location is required."), StringLength(150)]
    public string Location { get; set; } = "";

    [Required(ErrorMessage = "Distance is required."), Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km."), Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Please choose an event type."), StringLength(20), Display(Name = "Event type")]
    public string EventType { get; set; } = "";

    public string? BannerImagePath { get; set; }

    [Display(Name = "Event banner image")]
    public IFormFile? BannerFile { get; set; }

    public EventRequest ToRequest() =>
        new(Name, Description, EventDate, Location, DistanceKm ?? 0, EventType);
}

public class CategoryFormViewModel
{
    public int Id { get; set; }
    public int EventId { get; set; }

    [Required(ErrorMessage = "Category name is required."), StringLength(50)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Distance is required."), Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km."), Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Price is required (use 0 for free)."), Range(0, 100000, ErrorMessage = "Price must be 0 or more.")]
    public decimal? Price { get; set; }

    [Required(ErrorMessage = "Enter the maximum number of participants."), Range(1, 100000, ErrorMessage = "Enter at least 1."), Display(Name = "Max participants")]
    public int? MaxParticipants { get; set; }

    [Range(0, 120, ErrorMessage = "Enter an age between 0 and 120."), Display(Name = "Minimum age (optional)")]
    public int? MinAge { get; set; }

    [Range(0, 120, ErrorMessage = "Enter an age between 0 and 120."), Display(Name = "Maximum age (optional)")]
    public int? MaxAge { get; set; }

    public CategoryRequest ToRequest() => new(Name, DistanceKm ?? 0, Price ?? 0, MaxParticipants ?? 0, MinAge, MaxAge);
}

public class ResultFormViewModel
{
    public int EventId { get; set; }
    public int EnrollmentId { get; set; }
    public int? ResultId { get; set; }

    [Required(ErrorMessage = "Enter a finish time like 00:48:30.")]
    public TimeSpan? FinishTime { get; set; }

    [Required(ErrorMessage = "Enter the finishing position."), Range(1, 100000, ErrorMessage = "Position must be 1 or higher.")]
    public int? Position { get; set; }
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your full name."), StringLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    public string Email { get; set; } = "";

    [StringLength(20, ErrorMessage = "Phone number can be at most 20 characters."), Display(Name = "Phone number")]
    public string? Phone { get; set; }

    [DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true), Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(150), Display(Name = "Emergency contact")]
    public string? EmergencyContact { get; set; }

    [StringLength(100), Display(Name = "Club name")]
    public string? ClubName { get; set; }

    public string? ProfilePicturePath { get; set; }
}

public class EventPickerViewModel
{
    public string Title { get; set; } = "";
    public string Intro { get; set; } = "";
    public string Controller { get; set; } = "";
    public List<EventResponse> Events { get; set; } = new();
}

public class EventFilterViewModel
{
    public string? Search { get; set; }
    public string? Type { get; set; }
    public string? Location { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public bool IncludePast { get; set; }
}
