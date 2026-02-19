// ─── ExceptionMiddleware.cs ───────────────────────────────────────────────────
using EsotericStore.API.Models.DTOs;
using System.Text.Json;

namespace EsotericStore.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception switch
        {
            ArgumentException           => StatusCodes.Status400BadRequest,
            KeyNotFoundException        => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            _                           => StatusCodes.Status500InternalServerError
        };

        var response = ApiResponse<object>.Fail(
            context.Response.StatusCode == 500
                ? "Error interno del servidor"
                : exception.Message
        );

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}