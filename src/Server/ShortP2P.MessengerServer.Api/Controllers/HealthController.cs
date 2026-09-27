using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShortP2P.MessengerServer.Contracts;

namespace ShortP2P.MessengerServer.Api.Controllers;

[ApiController]
[Route($"{ApiRoutes.Prefix}/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet("ping")]
    [AllowAnonymous]
    public IActionResult Ping() => Ok();
}
