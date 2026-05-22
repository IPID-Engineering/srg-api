using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;

namespace SRG.Api.Middleware;

public class GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Brak dostępu."),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Zasób nie został znaleziony."),
            ValidationException validationEx => (HttpStatusCode.BadRequest, validationEx.Message),
            ArgumentException argEx => (HttpStatusCode.BadRequest, argEx.Message),
            InvalidOperationException invEx => (HttpStatusCode.BadRequest, invEx.Message),
            _ => (HttpStatusCode.InternalServerError, "Wystąpił nieoczekiwany błąd. Spróbuj ponownie później.")
        };

        logger.LogError(exception, "Unhandled exception occurred. Status: {StatusCode}, Path: {Path}", 
            (int)statusCode, context.Request.Path);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            message,
            status = (int)statusCode,
            timestamp = DateTime.UtcNow
        };

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}

public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
