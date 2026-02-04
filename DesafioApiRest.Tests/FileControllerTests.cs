using DesafioApiRest.Api.Controllers;
using DesafioApiRest.Api.Dtos.Response;
using DesafioApiRest.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DesafioApiRest.Tests;

public class FileControllerTests
{
    [Fact]
    public async Task ReadFile_DeveRetornarOk_QuandoArquivoExiste()
    {
        // Arrange
        var mockService = new Mock<IFileService>();
        var expectedResponse = new FileReadResponseDto("teste.txt", "Conteudo");

        mockService.Setup(s => s.ReadFileAsync("teste.txt", CancellationToken.None))
                   .ReturnsAsync(expectedResponse);

        var controller = new FileController(mockService.Object);

        // Act
        var result = await controller.ReadFile("teste.txt", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResponse, okResult.Value);
    }

    [Fact]
    public async Task ReadFile_DeveRetornarBadRequest_QuandoCaminhoInvalido()
    {
        // Arrange
        var mockService = new Mock<IFileService>();
        mockService.Setup(s => s.ReadFileAsync("", CancellationToken.None))
                   .ThrowsAsync(new ArgumentException("O caminho do arquivo não pode ser vazio."));

        var controller = new FileController(mockService.Object);

        // Act
        var result = await controller.ReadFile("", CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task ReadFile_DeveRetornarNotFound_QuandoArquivoNaoExiste()
    {
        // Arrange
        var mockService = new Mock<IFileService>();
        mockService.Setup(s => s.ReadFileAsync("inexistente.txt", CancellationToken.None))
                   .ThrowsAsync(new FileNotFoundException());

        var controller = new FileController(mockService.Object);

        // Act
        var result = await controller.ReadFile("inexistente.txt", CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task ReadFile_DeveRetornarForbid_QuandoTentarPathTraversal()
    {
        // Arrange
        var mockService = new Mock<IFileService>();
        mockService.Setup(s => s.ReadFileAsync("../../../Program.cs", CancellationToken.None))
                   .ThrowsAsync(new UnauthorizedAccessException());

        var controller = new FileController(mockService.Object);

        // Act
        var result = await controller.ReadFile("../../../Program.cs", CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task ReadFile_DeveRetornarInternalServerError_QuandoErroInesperado()
    {
        // Arrange
        var mockService = new Mock<IFileService>();
        mockService.Setup(s => s.ReadFileAsync("teste.txt", CancellationToken.None))
                   .ThrowsAsync(new Exception("Erro inesperado"));

        var controller = new FileController(mockService.Object);

        // Act
        var result = await controller.ReadFile("teste.txt", CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }
}
