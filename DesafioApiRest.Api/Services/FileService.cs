using DesafioApiRest.Api.Dtos.Response;
using DesafioApiRest.Api.Interfaces;

namespace DesafioApiRest.Api.Services;

public class FileService : IFileService
{
    private readonly string _basePath;

    public FileService()
    {
        _basePath = Path.Combine(Directory.GetCurrentDirectory(), "ArquivosSeguros");
        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);
            
        // Cria um arquivo de teste lá dentro para você não ter erro de "não encontrado" logo de cara
        File.WriteAllText(Path.Combine(_basePath, "teste.txt"), "Olá! Este é o conteúdo do arquivo seguro.");
    }

    public async Task<FileReadResponseDto> ReadFileAsync(string relativePath, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("O caminho do arquivo não pode ser vazio.");

        // SEGURANÇA: GetFullPath resolve os ".." e previne sair da pasta
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, relativePath));

        _ = (fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase), File.Exists(fullPath)) switch
        {
            (false, _) => throw new UnauthorizedAccessException("Tentativa de acesso ilegal a arquivo fora da pasta base."),
            (true, false) => throw new FileNotFoundException("Arquivo não encontrado."),
            (true, true) => true
        };

        var content = await File.ReadAllTextAsync(fullPath, ct);
        var fileName = Path.GetFileName(fullPath);

        return new FileReadResponseDto(fileName, content);
    }

    public async Task WriteFileAsync(string relativePath, string content, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("O caminho do arquivo não pode ser vazio.");

        if (content == null)
            throw new ArgumentNullException(nameof(content), "O conteúdo não pode ser nulo.");

        // SEGURANÇA: GetFullPath resolve os ".." e previne sair da pasta
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, relativePath));

        _ = fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase) switch
        {
            false => throw new UnauthorizedAccessException("Tentativa de acesso ilegal a arquivo fora da pasta base."),
            true => true
        };

        // Cria o diretório se não existir
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(fullPath, content, ct);
    }
}