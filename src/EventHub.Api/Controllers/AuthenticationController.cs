using EventHub.Api.Models;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Interfaces;
using EventHub.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticationController(IAuthService service) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResult>> Register([FromBody] RegisterUser body)
    {
        try
        {
            return Ok(await service.RegisterAsync(body.Email, body.Password, body.DisplayName, body.Role));
        }
        catch (AlreadyExistsException e)
        {
            Console.WriteLine(e);
            return Conflict(e.Message);
        }
        catch (BusinessRuleException e)
        {
            Console.WriteLine(e);
            return BadRequest(e.Message);
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResult>> Login([FromBody] LoginUser body)
    {
        try
        {
            return Ok(await service.LoginAsync(body.Email, body.Password));
        }
        catch (UnAuthorizedException e)
        {
            Console.WriteLine(e);
            return Unauthorized(e.Message);
        }
    }
}