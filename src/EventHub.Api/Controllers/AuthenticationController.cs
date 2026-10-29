using EventHub.Api.Authentication;
using EventHub.Api.Extensions;
using EventHub.Api.Models;
using EventHub.Domain.Interfaces;
using EventHub.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticationController(IAuthService service) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<CurrentUser>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CurrentUser>> Register([FromBody] RegisterUser body)
    {
        var result = await service.RegisterAsync(body.Email, body.Password, body.DisplayName);
        return Ok(SignIn(result));
    }

    [HttpPost("login")]
    [ProducesResponseType<CurrentUser>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUser>> Login([FromBody] LoginUser body)
    {
        var result = await service.LoginAsync(body.Email, body.Password);
        return Ok(SignIn(result));
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AuthCookie.Name, AuthCookie.Options());
        return NoContent();
    }


    [Authorize]
    [HttpGet("me")]
    public ActionResult<CurrentUser> Me() => User.ToCurrentUser();

    private CurrentUser SignIn(AuthResult result)
    {
        Response.Cookies.Append(AuthCookie.Name, result.AccessToken, AuthCookie.Options(result.ExpiresAt));
        return new CurrentUser
        {
            UserId = result.UserId,
            DisplayName = result.DisplayName,
            Roles = [.. result.Roles],
            ExpiresAt = result.ExpiresAt,
        };
    }


}