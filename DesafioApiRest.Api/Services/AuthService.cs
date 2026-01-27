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

        var jwtKey = _configuration["Jwt:Key"] 
            ?? throw new InvalidOperationException("Chave JWT não configurada no appsettings.json");
        var issuer = _configuration["Jwt:Issuer"] 
            ?? throw new InvalidOperationException("Issuer JWT não configurado no appsettings.json");
        var audience = _configuration["Jwt:Audience"] 
            ?? throw new InvalidOperationException("Audience JWT não configurado no appsettings.json");
        
        var expirationHoursString = _configuration["Jwt:ExpirationHours"] 
            ?? throw new InvalidOperationException("ExpirationHours JWT não configurado no appsettings.json");
        
        if (!int.TryParse(expirationHoursString, out var expirationHours) || expirationHours <= 0)
            throw new InvalidOperationException("ExpirationHours JWT deve ser um número inteiro positivo.");

        var key = Encoding.ASCII.GetBytes(jwtKey);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, loginDto.Username)
            }),
            Expires = DateTime.UtcNow.AddHours(expirationHours),
            // Issuer: Identifica quem emitiu o token (esta API)
            // Justificativa: Permite que serviços consumidores verifiquem a origem do token
            Issuer = issuer,
            // Audience: Identifica para quem o token foi emitido (clientes autorizados)
            // Justificativa: Previne que tokens sejam usados em sistemas não autorizados
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}