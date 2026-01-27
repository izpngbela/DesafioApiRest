using DesafioApiRest.Api.Dtos.Response;

namespace DesafioApiRest.Api.Interfaces;

public interface IFileService
{
    Task<FileReadResponseDto> ReadFileAsync(string relativePath, CancellationToken ct);
    Task WriteFileAsync(string relativePath, string content, CancellationToken ct);
}