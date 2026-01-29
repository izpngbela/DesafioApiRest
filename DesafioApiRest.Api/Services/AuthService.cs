using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DesafioApiRest.Api.Configuration;
using DesafioApiRest.Api.Dtos.Request;
using DesafioApiRest.Api.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DesafioApiRest.Api.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IPasswordHasher _passwordHasher;
    private readonly UserCredentialsOptions _userCredentials;

    public AuthService(
        IConfiguration configuration, 
        ILogger<AuthService> logger,
        IPasswordHasher passwordHasher,
        IOptions<UserCredentialsOptions> userCredentials)
    {
        _configuration = configuration;
        _logger = logger;
        _passwordHasher = passwordHasher;
        _userCredentials = userCredentials.Value;
    }

    public string? GenerateToken(LoginRequestDto loginDto)
    {
        _logger.LogInformation("Tentativa de login para o usuário: {Username}", loginDto.Username);
        
        // Busca o usuário na configuração
        var user = _userCredentials.AllowedUsers
            .FirstOrDefault(u => u.Username.Equals(loginDto.Username, StringComparison.OrdinalIgnoreCase));
        
        if (user == null)
        {
            _logger.LogWarning("Usuário não encontrado: {Username}", loginDto.Username);
            return null;
        }
        
        _logger.LogInformation("Verificando senha para usuário: {Username}", loginDto.Username);
        _logger.LogInformation("Hash armazenado: {Hash}", user.PasswordHash);
        
        bool senhaValida = _passwordHasher.VerifyPassword(loginDto.Password, user.PasswordHash);
        _logger.LogInformation("Resultado da verificação de senha: {Result}", senhaValida);
        
        if (!senhaValida)
        {
            _logger.LogWarning("Senha incorreta para o usuário: {Username}", loginDto.Username);
            return null;
        }

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
        var tokenString = tokenHandler.WriteToken(token);
        
        _logger.LogInformation("Token gerado com sucesso para o usuário: {Username}", loginDto.Username);
        
        return tokenString;
    }
}