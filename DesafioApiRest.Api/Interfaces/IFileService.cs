using DesafioApiRest.Api.Dtos;

namespace DesafioApiRest.Api.Interfaces;

public interface IFileService
{
    Task<FileReadResponseDto> ReadFileAsync(string relativePath, CancellationToken ct);
}