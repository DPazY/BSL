using BSL.Models;
using Microsoft.AspNetCore.Mvc;

namespace BSL.App.Endpoints;

public static class IngestionEndpoints
{
    public static IEndpointRouteBuilder MapIngestionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ingestion")
            .WithTags("Data Ingestion Pipeline");

        group.MapPost("/upload", async (
            IFormFile file,
            [FromServices] AppSettings appSettings,
            CancellationToken ct) =>
        {
            if (file.Length == 0)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Пустой файл",
                    Detail = "Переданный файл не содержит данных."
                });
            }

            // Сохраняем файл в директорию FileWatcher, чтобы сработал реактивный пайплайн
            string targetDir = appSettings.FileWatcherDirectory;
            Directory.CreateDirectory(targetDir);

            string safeFileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Path.GetFileName(file.FileName)}";
            string fullPath = Path.Combine(targetDir, safeFileName);

            await using (var stream = File.Create(fullPath))
            {
                await file.CopyToAsync(stream, ct);
            }

            return Results.Accepted(value: new
            {
                Message = "Файл передан в реактивный конвейер FileWatcher -> FileProcessingQueue",
                FileName = safeFileName,
                SizeBytes = file.Length
            });
        })
        .DisableAntiforgery()
        .WithSummary("Загрузить файл (XML/CSV/JSON) в реактивный конвейер обработки");

        return app;
    }
}