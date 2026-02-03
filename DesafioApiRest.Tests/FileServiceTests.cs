using DesafioApiRest.Api.Configuration;
using DesafioApiRest.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DesafioApiRest.Tests;

public class FileServiceTests : IDisposable
{
    private readonly FileService _fileService;
    private readonly Mock<ILogger<FileService>> _mockLogger;
    private readonly Mock<IOptions<FileServiceOptions>> _mockOptions;
    private readonly string _testBasePath;

    public FileServiceTests()
    {
        _mockLogger = new Mock<ILogger<FileService>>();
        _mockOptions = new Mock<IOptions<FileServiceOptions>>();
        _testBasePath = Path.Combine(Directory.GetCurrentDirectory(), "ArquivosSegurosTeste");
        
        var fileServiceOptions = new FileServiceOptions
        {
            BaseDirectory = _testBasePath,
            MaxFileSizeBytes = 10 * 1024 * 1024
        };
        _mockOptions.Setup(o => o.Value).Returns(fileServiceOptions);
        
        _fileService = new FileService(_mockLogger.Object, _mockOptions.Object);
        
        if (Directory.Exists(_testBasePath))
        {
            Directory.Delete(_testBasePath, true);
        }
        Directory.CreateDirectory(_testBasePath);
    }

    [Fact]
    public void ReadFile_DeveRetornarConteudo_QuandoArquivoExiste()
    {
        var testFile = "teste.txt";
        var testContent = "Conteúdo de teste";
        var fullPath = Path.Combine(_testBasePath, testFile);
        File.WriteAllText(fullPath, testContent);

        var result = _fileService.ReadFile(testFile);

        Assert.True(result.Success);
        Assert.Equal(testContent, result.Content);
    }

    [Fact]
    public void WriteFile_DeveCriarArquivo_QuandoDadosValidos()
    {
        var testFile = "novo.txt";
        var testContent = "Novo conteúdo";

        var result = _fileService.WriteFile(testFile, testContent);

        Assert.True(result.Success);
        var fullPath = Path.Combine(_testBasePath, testFile);
        Assert.True(File.Exists(fullPath));
        Assert.Equal(testContent, File.ReadAllText(fullPath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testBasePath))
        {
            Directory.Delete(_testBasePath, true);
        }
    }
}
