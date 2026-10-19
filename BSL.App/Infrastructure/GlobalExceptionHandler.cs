using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BSL.App.Infrastructure;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Ошибка при обработке HTTP-запроса {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        var (statusCode, title) = exception switch
        {
            ArgumentException or FormatException => (StatusCodes.Status400BadRequest, "Некорректные параметры запроса"),
            KeyNotFoundException or FileNotFoundException => (StatusCodes.Status404NotFound, "Ресурс не найден"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Доступ запрещён"),
            _ => (StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера")
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
        };

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}