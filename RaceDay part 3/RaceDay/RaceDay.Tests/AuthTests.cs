using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Tests;

public class AuthTests
{
    [Fact]
    public async Task Register_works_and_hashes_password()
    {
        var db = TestHelper.NewDb();
        var c = TestHelper.WithSession(new AuthController(db));

        var result = await c.Register(new RegisterDto("Sam Runner", "sam@test.com", "Secret123!", Roles.Participant));

        Assert.IsType<OkObjectResult>(result);
        var user = Assert.Single(db.Users);
        Assert.NotEqual("Secret123!", user.PasswordHash);
    }

    [Fact]
    public async Task Register_rejects_duplicate_email_and_bad_role()
    {
        var db = TestHelper.NewDb();
        var c = TestHelper.WithSession(new AuthController(db));
        await c.Register(new RegisterDto("Sam", "sam@test.com", "Secret123!", Roles.Participant));

        Assert.IsType<BadRequestObjectResult>(await c.Register(new RegisterDto("Sam2", "sam@test.com", "x", Roles.Participant)));
        Assert.IsType<BadRequestObjectResult>(await c.Register(new RegisterDto("Bob", "bob@test.com", "x", "Admin")));
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var db = TestHelper.NewDb();
        var c = TestHelper.WithSession(new AuthController(db));
        await c.Register(new RegisterDto("Sam", "sam@test.com", "Secret123!", Roles.Participant));

        var result = await c.Login(new LoginDto("sam@test.com", "WrongPassword"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_with_correct_password_stores_role_in_session()
    {
        var db = TestHelper.NewDb();
        var c = TestHelper.WithSession(new AuthController(db));
        await c.Register(new RegisterDto("Olivia", "o@test.com", "Secret123!", Roles.Organiser));

        var result = await c.Login(new LoginDto("o@test.com", "Secret123!"));

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(Roles.Organiser, c.HttpContext.Session.GetString("Role"));
        Assert.NotNull(c.HttpContext.Session.GetInt32("UserId"));
    }

    [Fact]
    public async Task Logout_clears_session()
    {
        var db = TestHelper.NewDb();
        var c = TestHelper.WithSession(new AuthController(db), Roles.Organiser, 1);

        c.Logout();

        Assert.Null(c.HttpContext.Session.GetString("Role"));
    }

    [Fact]
    public async Task Register_accepts_capitalised_role_and_stores_it_lowercase()
    {
        var db = TestHelper.NewDb();
        var c = TestHelper.WithSession(new AuthController(db));

        var result = await c.Register(new RegisterDto("Sam", "sam@test.com", "Secret123!", "Participant"));

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("participant", db.Users.Single().Role);
    }
}
