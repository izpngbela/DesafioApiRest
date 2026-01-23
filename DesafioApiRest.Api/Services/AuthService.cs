using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DesafioApiRest.Api.Dtos.Request;
using DesafioApiRest.Api.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace DesafioApiRest.Api.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;

    public AuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string? GenerateToken(LoginRequestDto loginDto)
    {
        // Validação fixa (conforme pedido no desafio)
        if (loginDto.Username != "admin" || loginDto.Password != "123456")
            return null;

        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "chave_super_secreta_para_teste_local_256bits");
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, loginDto.Username)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}