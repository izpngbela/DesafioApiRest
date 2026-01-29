using Asp.Versioning;
using DesafioApiRest.Api.Dtos.Request;
using DesafioApiRest.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DesafioApiRest.Api.Controllers;


[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

   
    [HttpPost("login")]
    [EnableRateLimiting("LoginPolicy")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public IActionResult Login([FromBody] LoginRequestDto loginDto)
    {
        var token = _authService.GenerateToken(loginDto);

        if (token == null)
            return Unauthorized(new { message = "Usuário ou senha inválidos." });

        return Ok(new { token });
    }
}