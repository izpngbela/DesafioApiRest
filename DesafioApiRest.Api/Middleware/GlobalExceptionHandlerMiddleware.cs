using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace DesafioApiRest.Api.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu uma exceção não tratada: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problemDetails = exception switch
        {
            ArgumentException or ArgumentNullException => new ProblemDetails
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Requisição inválida",
                Detail = exception.Message,
                Instance = context.Request.Path
            },
            UnauthorizedAccessException => new ProblemDetails
            {
                Status = (int)HttpStatusCode.Forbidden,
                Title = "Acesso negado",
                Detail = "Você não tem permissão para acessar este recurso.",
                Instance = context.Request.Path
            },
            FileNotFoundException => new ProblemDetails
            {
                Status = (int)HttpStatusCode.NotFound,
                Title = "Recurso não encontrado",
                Detail = exception.Message,
                Instance = context.Request.Path
            },
            _ => new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Erro interno no servidor",
                Detail = _env.IsDevelopment() ? exception.Message : "Ocorreu um erro inesperado. Tente novamente mais tarde.",
                Instance = context.Request.Path
            }
        };

        // Adiciona informações extras em desenvolvimento
        if (_env.IsDevelopment() && exception is not ArgumentException and not ArgumentNullException)
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
        }

        context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return context.Response.WriteAsJsonAsync(problemDetails, options);
    }
}
