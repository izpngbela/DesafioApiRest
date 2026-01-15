namespace DesafioApiRest.Api.Dtos;

public record FileReadRequestDto(string Path);
public record FileReadResponseDto(string FileName, string Content);