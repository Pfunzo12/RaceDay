using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Tests;

public class EventTests
{
    [Fact]
    public async Task Logged_out_user_gets_401()
    {
        var db = TestHelper.NewDb();
        var c = TestHelper.WithSession(new EventsController(db));

        var result = await c.Create(TestHelper.NewEventDto("Fun Run"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Participant_cannot_create_event()
    {
        var db = TestHelper.NewDb();
        var c = TestHelper.WithSession(new EventsController(db), Roles.Participant, 1);

        var result = await c.Create(TestHelper.NewEventDto("Fun Run"));

        var status = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(403, status.StatusCode);
    }

    [Fact]
    public async Task Participant_cannot_edit_or_delete_event()
    {
        var db = TestHelper.NewDb();
        TestHelper.SeedBasics(db);
        var c = TestHelper.WithSession(new EventsController(db), Roles.Participant, 2);

        var edit = Assert.IsAssignableFrom<ObjectResult>(await c.Update(1, TestHelper.NewEventDto("X")));
        var del = Assert.IsAssignableFrom<ObjectResult>(await c.Delete(1));

        Assert.Equal(403, edit.StatusCode);
        Assert.Equal(403, del.StatusCode);
        Assert.Single(db.Events);
    }

    [Fact]
    public async Task Organiser_can_create_event()
    {
        var db = TestHelper.NewDb();
        var c = TestHelper.WithSession(new EventsController(db), Roles.Organiser, 1);

        var result = await c.Create(TestHelper.NewEventDto());

        var status = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(201, status.StatusCode);
        Assert.Single(db.Events);
    }

    [Fact]
    public async Task Organiser_can_edit_event()
    {
        var db = TestHelper.NewDb();
        TestHelper.SeedBasics(db);
        var c = TestHelper.WithSession(new EventsController(db), Roles.Organiser, 1);

        var result = await c.Update(1, TestHelper.NewEventDto("Renamed 10K"));

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Renamed 10K", db.Events.Single().Name);
    }

    [Fact]
    public async Task Organiser_can_delete_event()
    {
        var db = TestHelper.NewDb();
        TestHelper.SeedBasics(db);
        var c = TestHelper.WithSession(new EventsController(db), Roles.Organiser, 1);

        var result = await c.Delete(1);

        Assert.IsType<OkObjectResult>(result);
        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task Deleting_event_also_removes_its_categories_and_enrolments()
    {
        var db = TestHelper.NewDb();
        TestHelper.SeedBasics(db);
        TestHelper.SeedPastEvent(db);
        var c = TestHelper.WithSession(new EventsController(db), Roles.Organiser, 1);

        var result = await c.Delete(2);

        Assert.IsType<OkObjectResult>(result);
        Assert.Single(db.Events);
        Assert.Single(db.Categories);
        Assert.Empty(db.EventEnrollments);
    }

    [Fact]
    public async Task Update_unknown_event_returns_404()
    {
        var db = TestHelper.NewDb();
        var c = TestHelper.WithSession(new EventsController(db), Roles.Organiser, 1);

        var result = await c.Update(999, TestHelper.NewEventDto("X"));

        Assert.IsType<NotFoundObjectResult>(result);
    }
}

public class EventFilterAndDashboardTests
{
    [Fact]
    public async Task Create_rejects_missing_event_type_or_zero_distance()
    {
        var db = TestHelper.NewDb();
        var c = TestHelper.WithSession(new EventsController(db), Roles.Organiser, 1);

        Assert.IsType<BadRequestObjectResult>(await c.Create(new EventDto("A", "B", DateTime.Today, "C", 10m, "")));
        Assert.IsType<BadRequestObjectResult>(await c.Create(new EventDto("A", "B", DateTime.Today, "C", 0m, "Road Run")));
        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task GetAll_filters_by_type_and_upcoming()
    {
        var db = TestHelper.NewDb();
        TestHelper.SeedBasics(db);       // upcoming Road Run
        TestHelper.SeedPastEvent(db);    // past Fun Run
        var c = TestHelper.WithSession(new EventsController(db), Roles.Participant, 2);

        var all = Assert.IsType<OkObjectResult>(await c.GetAll());
        var upcoming = Assert.IsType<OkObjectResult>(await c.GetAll(upcoming: true));
        var fun = Assert.IsType<OkObjectResult>(await c.GetAll(type: "Fun Run"));

        Assert.Equal(2, Assert.IsAssignableFrom<IEnumerable<EventResponse>>(all.Value).Count());
        Assert.Single(Assert.IsAssignableFrom<IEnumerable<EventResponse>>(upcoming.Value));
        Assert.Single(Assert.IsAssignableFrom<IEnumerable<EventResponse>>(fun.Value));
    }

    [Fact]
    public async Task Dashboard_counts_events_and_enrolments()
    {
        var db = TestHelper.NewDb();
        TestHelper.SeedBasics(db);
        TestHelper.SeedPastEvent(db);
        var c = TestHelper.WithSession(new OrganiserController(db), Roles.Organiser, 1);

        var ok = Assert.IsType<OkObjectResult>(await c.Dashboard());

        var dash = Assert.IsType<OrganiserDashboardResponse>(ok.Value);
        Assert.Equal(2, dash.TotalEvents);
        Assert.Equal(1, dash.TotalEnrolments);
        Assert.Equal(1, dash.UpcomingEvents);
    }

    [Fact]
    public async Task Participant_cannot_open_dashboard()
    {
        var db = TestHelper.NewDb();
        var c = TestHelper.WithSession(new OrganiserController(db), Roles.Participant, 2);

        var result = Assert.IsAssignableFrom<ObjectResult>(await c.Dashboard());

        Assert.Equal(403, result.StatusCode);
    }
}
