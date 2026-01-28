using Asp.Versioning;
using DesafioApiRest.Api.Dtos.Request;
using DesafioApiRest.Api.Dtos.Response;
using DesafioApiRest.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DesafioApiRest.Api.Controllers;

/// Controller responsável pelo gerenciamento seguro de arquivos
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize] // Exige Token JWT
public class FileController : ControllerBase
{
    private readonly IFileService _fileService;

    public FileController(IFileService fileService)
    {
        _fileService = fileService;
    }

    
    [HttpGet("read")]
    [EnableRateLimiting("FilePolicy")]
    [ProducesResponseType(typeof(FileReadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ReadFile([FromQuery] string path, CancellationToken ct)
    {
        var result = await _fileService.ReadFileAsync(path, ct);
        return Ok(result);
    }

    [HttpPost("write")]
    [EnableRateLimiting("FilePolicy")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> WriteFile([FromBody] FileWriteRequestDto request, CancellationToken ct)
    {
        await _fileService.WriteFileAsync(request.Path, request.Content, ct);
        return Ok(new { message = "Arquivo gravado com sucesso.", path = request.Path });
    }
}