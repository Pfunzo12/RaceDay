using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Controllers;
using RaceDay.Web.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Areas.Participant.Controllers;

[Area("Participant")]
[RoleRequired("participant")]
public abstract class ParticipantBaseController : AppController
{
    protected ParticipantBaseController(ApiClient api) : base(api) { }
}
