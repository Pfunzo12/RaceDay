using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Tests;

public class ResultTests
{
    [Fact]
    public async Task Cannot_capture_result_before_event_concludes()
    {
        var db = TestHelper.NewDb();
        TestHelper.SeedBasics(db);   // event 1 is 30 days in the future
        db.EventEnrollments.Add(new EventEnrollment { Id = 5, UserId = 2, CategoryId = 1, Status = EnrolmentStatus.Confirmed });
        db.SaveChanges();
        var c = TestHelper.WithSession(new ResultsController(db), Roles.Organiser, 1);

        var result = await c.Capture(new CaptureResultDto(5, TimeSpan.FromMinutes(50), 1));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.Results);
    }

    [Fact]
    public async Task Organiser_can_capture_result_after_event()
    {
        var db = TestHelper.NewDb();
        TestHelper.SeedBasics(db);
        TestHelper.SeedPastEvent(db);
        var c = TestHelper.WithSession(new ResultsController(db), Roles.Organiser, 1);

        var result = Assert.IsAssignableFrom<ObjectResult>(await c.Capture(new CaptureResultDto(1, TimeSpan.FromMinutes(24), 3)));

        Assert.Equal(201, result.StatusCode);
        Assert.False(db.Results.Single().IsPublished);
    }

    [Fact]
    public async Task Pending_enrolment_cannot_have_result()
    {
        var db = TestHelper.NewDb();
        TestHelper.SeedBasics(db);
        TestHelper.SeedPastEvent(db);
        db.EventEnrollments.Single().Status = EnrolmentStatus.Pending;
        db.SaveChanges();
        var c = TestHelper.WithSession(new ResultsController(db), Roles.Organiser, 1);

        var result = await c.Capture(new CaptureResultDto(1, TimeSpan.FromMinutes(24), 3));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Participant_cannot_capture_results()
    {
        var db = TestHelper.NewDb();
        TestHelper.SeedBasics(db);
        TestHelper.SeedPastEvent(db);
        var c = TestHelper.WithSession(new ResultsController(db), Roles.Participant, 2);

        var result = Assert.IsAssignableFrom<ObjectResult>(await c.Capture(new CaptureResultDto(1, TimeSpan.FromMinutes(24), 3)));

        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task Publish_makes_results_visible_to_participant_with_total_finishers()
    {
        var db = TestHelper.NewDb();
        TestHelper.SeedBasics(db);
        TestHelper.SeedPastEvent(db);
        db.Users.Add(new User { Id = 3, FullName = "Other Runner", Email = "x@test.com", Role = Roles.Participant });
        db.EventEnrollments.Add(new EventEnrollment { Id = 2, UserId = 3, CategoryId = 2, Status = EnrolmentStatus.Confirmed });
        db.Results.Add(new RaceResult { EnrollmentId = 1, FinishTime = TimeSpan.FromMinutes(24), Position = 2 });
        db.Results.Add(new RaceResult { EnrollmentId = 2, FinishTime = TimeSpan.FromMinutes(22), Position = 1 });
        db.SaveChanges();

        var org = TestHelper.WithSession(new ResultsController(db), Roles.Organiser, 1);
        var participant = TestHelper.WithSession(new ResultsController(db), Roles.Participant, 2);

        // before publishing: nothing visible
        var before = Assert.IsType<OkObjectResult>(await participant.Mine());
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<ResultResponse>>(before.Value));

        Assert.IsType<OkObjectResult>(await org.Publish(2));

        var after = Assert.IsType<OkObjectResult>(await participant.Mine());
        var row = Assert.Single(Assert.IsAssignableFrom<IEnumerable<ResultResponse>>(after.Value));
        Assert.Equal(2, row.Position);
        Assert.Equal(2, row.TotalFinishers);
    }
}
