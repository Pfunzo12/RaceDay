namespace RaceDay.Web.Models;

// Requests sent to the API
public record LoginRequest(string Email, string Password);
public record RegisterRequest(string FullName, string Email, string Password, string Role);
public record EventRequest(string Name, string Description, DateTime EventDate, string Location, decimal DistanceKm, string EventType);
public record CategoryRequest(string Name, decimal DistanceKm, decimal Price, int MaxParticipants, int? MinAge, int? MaxAge);
public record EnrolRequest(int CategoryId);
public record EnrolmentStatusRequest(string Status);
public record ProfileUpdateRequest(string FullName, string? Phone, DateTime? DateOfBirth, string? EmergencyContact, string? ClubName);
public record CaptureResultRequest(int EnrollmentId, TimeSpan FinishTime, int Position);
public record UpdateResultRequest(TimeSpan FinishTime, int Position);

// Responses read from the API
public record LoginResponse(int UserId, string FullName, string Role);
public record EventResponse(int Id, string Name, string Description, DateTime EventDate, string Location,
    decimal DistanceKm, string EventType, string? BannerImagePath, int OrganiserId);
public record CategoryResponse(int Id, int EventId, string Name, decimal DistanceKm, decimal Price,
    int MaxParticipants, int Taken, int? MinAge, int? MaxAge);
public record EnrolmentResponse(int Id, int EventId, string EventName, DateTime EventDate, string Location,
    int CategoryId, string CategoryName, string Status, DateTime EnrolledOn);
public record EventEnrolmentResponse(int Id, int UserId, string ParticipantName, string Email,
    int CategoryId, string CategoryName, string Status, DateTime EnrolledOn);
public record ResultEntryRow(int EnrollmentId, string ParticipantName, string CategoryName,
    int? ResultId, TimeSpan? FinishTime, int? Position, bool IsPublished);
public record ResultResponse(int Id, int EnrollmentId, string EventName, DateTime EventDate, string CategoryName,
    TimeSpan FinishTime, int Position, int TotalFinishers);
public record ProfileResponse(int UserId, string FullName, string Email, string Role, string? Phone,
    DateTime? DateOfBirth, string? EmergencyContact, string? ClubName, string? ProfilePicturePath);
public record DashboardEventRow(int Id, string Name, DateTime EventDate, string Location,
    int EnrolmentCount, int ConfirmedCount, bool IsUpcoming);
public record OrganiserDashboardResponse(int TotalEvents, int TotalEnrolments, int UpcomingEvents,
    List<DashboardEventRow> Events);
