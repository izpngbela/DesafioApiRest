using DesafioApiRest.Api.Dtos;
using DesafioApiRest.Api.Interfaces;

namespace DesafioApiRest.Api.Services;

public class FileService : IFileService
{
    private readonly string _basePath;

    public FileService()
    {
        // Cria uma pasta chamada "ArquivosSeguros" na raiz da API para teste
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

        if (!fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Tentativa de acesso ilegal a arquivo fora da pasta base.");
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Arquivo não encontrado.");
        }

        var content = await File.ReadAllTextAsync(fullPath, ct);
        var fileName = Path.GetFileName(fullPath);

        return new FileReadResponseDto(fileName, content);
    }
}