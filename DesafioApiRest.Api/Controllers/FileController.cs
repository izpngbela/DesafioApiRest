using DesafioApiRest.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DesafioApiRest.Api.Dtos.Request;

namespace DesafioApiRest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Exige Token JWT
public class FileController : ControllerBase
{
    private readonly IFileService _fileService;

    public FileController(IFileService fileService)
    {
        _fileService = fileService;
    }

    [HttpGet("read")]
    public async Task<IActionResult> ReadFile([FromQuery] string path, CancellationToken ct)
    {
        try
        {
            var result = await _fileService.ReadFileAsync(path, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            // Retorna 403 Forbidden se tentar sair da pasta
            return Forbid();
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Arquivo solicitado não existe." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Erro interno no servidor." });
        }
    }

    [HttpPost("write")]
    public async Task<IActionResult> WriteFile([FromBody] FileWriteRequestDto request, CancellationToken ct)
    {
        try
        {
            await _fileService.WriteFileAsync(request.Path, request.Content, ct);
            return Ok(new { message = "Arquivo gravado com sucesso.", path = request.Path });
        }
        catch (ArgumentNullException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            // Retorna 403 Forbidden se tentar sair da pasta
            return Forbid();
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Erro interno no servidor." });
        }
    }
}