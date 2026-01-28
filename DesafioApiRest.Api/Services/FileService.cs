using DesafioApiRest.Api.Configuration;
using DesafioApiRest.Api.Dtos.Response;
using DesafioApiRest.Api.Interfaces;
using Microsoft.Extensions.Options;

namespace DesafioApiRest.Api.Services;

public class FileService : IFileService
{
    private readonly string _basePath;
    private readonly ILogger<FileService> _logger;
    private readonly long _maxFileSize;

    public FileService(ILogger<FileService> logger, IOptions<FileServiceOptions> options)
    {
        _logger = logger;
        var config = options.Value;
        
        _basePath = Path.IsPathRooted(config.BasePath) 
            ? config.BasePath 
            : Path.Combine(Directory.GetCurrentDirectory(), config.BasePath);
            
        _maxFileSize = config.MaxFileSize;
        
        EnsureBasePathExists();
    }
    
    private void EnsureBasePathExists()
    {
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation("Diretório base criado em: {BasePath}", _basePath);
        }
    }

    public async Task<FileReadResponseDto> ReadFileAsync(string relativePath, CancellationToken ct)
    {
        _logger.LogInformation("Solicitação de leitura de arquivo: {RelativePath}", relativePath);
        
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("O caminho do arquivo não pode ser vazio.");

        var fullPath = ValidateAndGetFullPath(relativePath);
        
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Arquivo não encontrado.");
        
        _logger.LogDebug("Lendo arquivo: {FullPath}", fullPath);

        var content = await File.ReadAllTextAsync(fullPath, ct);
        var fileName = Path.GetFileName(fullPath);
        
        _logger.LogInformation("Arquivo lido com sucesso: {FileName}, Tamanho: {Size} bytes", fileName, content.Length);

        return new FileReadResponseDto(fileName, content);
    }

    public async Task WriteFileAsync(string relativePath, string content, CancellationToken ct)
    {
        _logger.LogInformation("Solicitação de escrita de arquivo: {RelativePath}", relativePath);
        
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("O caminho do arquivo não pode ser vazio.");

        if (content == null)
            throw new ArgumentNullException(nameof(content), "O conteúdo não pode ser nulo.");
        
        // Validação de tamanho do conteúdo
        if (content.Length > _maxFileSize)
        {
            _logger.LogWarning("Tentativa de gravar arquivo muito grande: {Size} bytes (máximo: {MaxSize})", 
                content.Length, _maxFileSize);
            throw new ArgumentException($"O conteúdo não pode exceder {_maxFileSize / 1024 / 1024} MB.");
        }

        var fullPath = ValidateAndGetFullPath(relativePath);

        // Cria o diretório se não existir
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.LogInformation("Diretório criado: {Directory}", directory);
        }

        await File.WriteAllTextAsync(fullPath, content, ct);
        
        _logger.LogInformation("Arquivo gravado com sucesso: {RelativePath}, Tamanho: {Size} bytes", 
            relativePath, content.Length);
    }
    
   
    /// Valida e retorna o caminho completo do arquivo, garantindo que está dentro do basePath
    private string ValidateAndGetFullPath(string relativePath)
    {
        // SEGURANÇA: GetFullPath resolve os ".." e previne path traversal
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, relativePath));
        
        if (!fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Tentativa de acesso ilegal detectada: {RelativePath}", relativePath);
            throw new UnauthorizedAccessException("Tentativa de acesso ilegal a arquivo fora da pasta base.");
        }
        
        return fullPath;
    }
}