using DesafioApiRest.Api.Dtos;

namespace DesafioApiRest.Api.Interfaces;

public interface IAuthService
{
    string? GenerateToken(LoginRequestDto loginDto);
}