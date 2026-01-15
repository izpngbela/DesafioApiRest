using DesafioApiRest.Api.Dtos;
using DesafioApiRest.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DesafioApiRest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequestDto loginDto)
    {
        var token = _authService.GenerateToken(loginDto);

        if (token == null)
            return Unauthorized(new { message = "Usuário ou senha inválidos." });

        return Ok(new { token });
    }
}