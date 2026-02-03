using DesafioApiRest.Api.Configuration;
using DesafioApiRest.Api.Dtos.Request;
using DesafioApiRest.Api.Interfaces;
using DesafioApiRest.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DesafioApiRest.Tests;

public class AuthServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IOptions<UserCredentialsOptions>> _mockOptions;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns("chave_super_secreta_para_teste_local_256bits");
        _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("DesafioApiRest");
        _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("DesafioApiRestClient");
        _mockConfiguration.Setup(c => c["Jwt:ExpireMinutes"]).Returns("60");
        
        _mockLogger = new Mock<ILogger<AuthService>>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockOptions = new Mock<IOptions<UserCredentialsOptions>>();
        
        var userCredentials = new UserCredentialsOptions
        {
            AllowedUsers = new List<UserCredential>
            {
                new UserCredential { Username = "admin", PasswordHash = "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYzpLaOke4S" }
            }
        };
        _mockOptions.Setup(o => o.Value).Returns(userCredentials);
        _mockPasswordHasher.Setup(p => p.VerifyPassword("123456", It.IsAny<string>())).Returns(true);
        _mockPasswordHasher.Setup(p => p.VerifyPassword(It.Is<string>(s => s != "123456"), It.IsAny<string>())).Returns(false);
        
        _authService = new AuthService(_mockConfiguration.Object, _mockLogger.Object, _mockPasswordHasher.Object, _mockOptions.Object);
    }

    [Fact]
    public void GenerateToken_DeveRetornarToken_QuandoCredenciaisValidas()
    {
        // Arrange
        var loginDto = new LoginRequestDto("admin", "123456");

        // Act
        var token = _authService.GenerateToken(loginDto);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateToken_DeveRetornarNull_QuandoUsuarioInvalido()
    {
        // Arrange
        var loginDto = new LoginRequestDto("user", "123456");

        // Act
        var token = _authService.GenerateToken(loginDto);

        // Assert
        Assert.Null(token);
    }

    [Fact]
    public void GenerateToken_DeveRetornarNull_QuandoSenhaInvalida()
    {
        // Arrange
        var loginDto = new LoginRequestDto("admin", "senha_errada");

        // Act
        var token = _authService.GenerateToken(loginDto);

        // Assert
        Assert.Null(token);
    }

    [Fact]
    public void GenerateToken_DeveRetornarNull_QuandoAmbosInvalidos()
    {
        // Arrange
        var loginDto = new LoginRequestDto("user_errado", "senha_errada");

        // Act
        var token = _authService.GenerateToken(loginDto);

        // Assert
        Assert.Null(token);
    }

    [Theory]
    [InlineData("", "123456")]
    [InlineData("admin", "")]
    [InlineData("", "")]
    public void GenerateToken_DeveRetornarNull_QuandoCredenciaisVazias(string username, string password)
    {
        // Arrange
        var loginDto = new LoginRequestDto(username, password);

        // Act
        var token = _authService.GenerateToken(loginDto);

        // Assert
        Assert.Null(token);
    }
}
