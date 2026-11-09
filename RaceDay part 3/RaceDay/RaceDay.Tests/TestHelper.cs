using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Tests;

public static class TestHelper
{
    public static RaceDayDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())   // fresh empty database per test
            .Options;
        return new RaceDayDbContext(options);
    }

    public static T WithSession<T>(T controller, string? role = null, int? userId = null) where T : ControllerBase
    {
        var http = new DefaultHttpContext { Session = new TestSession() };
        if (role != null) http.Session.SetString("Role", role);
        if (userId != null) http.Session.SetInt32("UserId", userId.Value);
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        return controller;
    }

    /// Seeds one organiser (Id 1), one participant (Id 2), one event (Id 1) and one category (Id 1).
    public static void SeedBasics(RaceDayDbContext db)
    {
        db.Users.Add(new User { Id = 1, FullName = "Olivia Organiser", Email = "o@test.com", Role = Roles.Organiser });
        db.Users.Add(new User { Id = 2, FullName = "Pat Participant", Email = "p@test.com", Role = Roles.Participant });
        db.Events.Add(new RaceEvent { Id = 1, Name = "City 10K", Description = "Fast city race", Location = "Pretoria",
            EventDate = DateTime.Today.AddDays(30), DistanceKm = 10, EventType = "Road Run", OrganiserId = 1 });
        db.Categories.Add(new Category { Id = 1, EventId = 1, Name = "10K Open", DistanceKm = 10 });
        db.SaveChanges();
    }

    /// Adds a finished event (Id 2, yesterday), its category (Id 2) and a Confirmed enrolment (Id 1) for participant 2.
    public static void SeedPastEvent(RaceDayDbContext db)
    {
        db.Events.Add(new RaceEvent { Id = 2, Name = "Old 5K", Description = "Done", Location = "Cape Town",
            EventDate = DateTime.Today.AddDays(-1), DistanceKm = 5, EventType = "Fun Run", OrganiserId = 1 });
        db.Categories.Add(new Category { Id = 2, EventId = 2, Name = "5K Fun Run", DistanceKm = 5 });
        db.EventEnrollments.Add(new EventEnrollment { Id = 1, UserId = 2, CategoryId = 2, Status = EnrolmentStatus.Confirmed });
        db.SaveChanges();
    }

    public static Api.Dtos.EventDto NewEventDto(string name = "City 10K") =>
        new(name, "A great race", DateTime.Today.AddDays(30), "Pretoria", 10m, "Road Run");
}
