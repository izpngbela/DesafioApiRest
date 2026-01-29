using DesafioApiRest.Api.Dtos.Request;

namespace DesafioApiRest.Api.Interfaces;

public interface IAuthService
{
    string? GenerateToken(LoginRequestDto loginDto);
}