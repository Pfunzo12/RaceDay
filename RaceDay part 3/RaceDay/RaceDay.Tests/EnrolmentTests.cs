using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Tests;

public class EnrolmentTests
{
    [Fact]
    public async Task Participant_can_enrol_in_valid_category()
    {
        var db = TestHelper.NewDb();
        TestHelper.SeedBasics(db);
        var c = TestHelper.WithSession(new EventsController(db), Roles.Participant, 2);

        var result = await c.Enrol(1, new EnrolDto(1));

        Assert.IsType<OkObjectResult>(result);
        Assert.Single(db.EventEnrollments);
    }

    [Fact]
    public async Task Duplicate_enrolment_is_rejected()
    {
        var db = TestHelper.NewDb();
        TestHelper.SeedBasics(db);
        var c = TestHelper.WithSession(new EventsController(db), Roles.Participant, 2);
        await c.Enrol(1, new EnrolDto(1));

        var result = await c.Enrol(1, new EnrolDto(1));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Single(db.EventEnrollments);
    }

    [Fact]
    public async Task Enrolling_in_unknown_category_returns_404()
    {
        var db = TestHelper.NewDb();
        TestHelper.SeedBasics(db);
        var c = TestHelper.WithSession(new EventsController(db), Roles.Participant, 2);

        var result = await c.Enrol(1, new EnrolDto(999));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Organiser_cannot_enrol()
    {
        var db = TestHelper.NewDb();
        TestHelper.SeedBasics(db);
        var c = TestHelper.WithSession(new EventsController(db), Roles.Organiser, 1);

        var result = Assert.IsAssignableFrom<ObjectResult>(await c.Enrol(1, new EnrolDto(1)));

        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task My_enrolments_only_returns_own_rows()
    {
        var db = TestHelper.NewDb();
        TestHelper.SeedBasics(db);
        db.Users.Add(new User { Id = 3, FullName = "Other", Email = "x@test.com", Role = Roles.Participant });
        db.EventEnrollments.Add(new EventEnrollment { UserId = 2, CategoryId = 1 });
        db.EventEnrollments.Add(new EventEnrollment { UserId = 3, CategoryId = 1 });
        db.SaveChanges();
        var c = TestHelper.WithSession(new EnrolmentsController(db), Roles.Participant, 2);

        var ok = Assert.IsType<OkObjectResult>(await c.Mine());

        var rows = Assert.IsAssignableFrom<IEnumerable<EnrolmentResponse>>(ok.Value);
        Assert.Single(rows);
    }

    [Fact]
    public async Task New_enrolment_starts_as_Pending()
    {
        var db = TestHelper.NewDb();
        TestHelper.SeedBasics(db);
        var c = TestHelper.WithSession(new EventsController(db), Roles.Participant, 2);

        await c.Enrol(1, new EnrolDto(1));

        Assert.Equal(EnrolmentStatus.Pending, db.EventEnrollments.Single().Status);
    }

    [Fact]
    public async Task Cannot_enrol_in_event_that_already_happened()
    {
        var db = TestHelper.NewDb();
        TestHelper.SeedBasics(db);
        TestHelper.SeedPastEvent(db);
        var c = TestHelper.WithSession(new EventsController(db), Roles.Participant, 3);

        var result = await c.Enrol(2, new EnrolDto(2));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Organiser_can_confirm_enrolment()
    {
        var db = TestHelper.NewDb();
        TestHelper.SeedBasics(db);
        db.EventEnrollments.Add(new EventEnrollment { Id = 1, UserId = 2, CategoryId = 1 });
        db.SaveChanges();
        var c = TestHelper.WithSession(new EnrolmentsController(db), Roles.Organiser, 1);

        var result = await c.SetStatus(1, new EnrolmentStatusDto(EnrolmentStatus.Confirmed));

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(EnrolmentStatus.Confirmed, db.EventEnrollments.Single().Status);
    }

    [Fact]
    public async Task Participant_cannot_change_enrolment_status()
    {
        var db = TestHelper.NewDb();
        TestHelper.SeedBasics(db);
        db.EventEnrollments.Add(new EventEnrollment { Id = 1, UserId = 2, CategoryId = 1 });
        db.SaveChanges();
        var c = TestHelper.WithSession(new EnrolmentsController(db), Roles.Participant, 2);

        var result = Assert.IsAssignableFrom<ObjectResult>(await c.SetStatus(1, new EnrolmentStatusDto(EnrolmentStatus.Confirmed)));

        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task Full_category_rejects_new_enrolment()
    {
        var db = TestHelper.NewDb();
        TestHelper.SeedBasics(db);
        db.Categories.Single().MaxParticipants = 1;
        db.Users.Add(new User { Id = 3, FullName = "Other", Email = "x@test.com", Role = Roles.Participant });
        db.EventEnrollments.Add(new EventEnrollment { UserId = 3, CategoryId = 1, Status = EnrolmentStatus.Confirmed });
        db.SaveChanges();
        var c = TestHelper.WithSession(new EventsController(db), Roles.Participant, 2);

        var result = await c.Enrol(1, new EnrolDto(1));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Single(db.EventEnrollments);
    }
}
