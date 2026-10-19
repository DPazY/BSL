using System.Diagnostics;
using BSL.Implementation.Repository;
using BSL.Models;
using BSL.Models.Dto;
using BSL.Models.Interface;
using Microsoft.AspNetCore.Mvc;

namespace BSL.App.Endpoints;

public static class CacheEndpoints
{
    public static IEndpointRouteBuilder MapCacheEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/cache")
            .WithTags("IFS vs LRU Predictive Cache");

        // 1. Принудительный прогрев кэша (Warmup)
        group.MapPost("/warmup", async (
            [FromQuery] int topCount,
            [FromServices] IRepository repository) =>
        {
            int count = Math.Clamp(topCount <= 0 ? 10 : topCount, 1, 100);
            var sw = Stopwatch.StartNew();

            var allBooks = (await repository.GetAll<Book>()).Take(count).ToList();
            foreach (var book in allBooks)
            {
                _ = await repository.GetByName<Book>(book.Name);
            }

            sw.Stop();
            return Results.Ok(new
            {
                Message = "Упреждающий прогрев IFS-кэша выполнен",
                WarmedCount = allBooks.Count,
                ElapsedMs = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
                Keys = allBooks.Select(b => b.Name).ToList()
            });
        })
        .WithSummary("Прогреть IFS-кэш топ-N изданиями");

        // 2. Сравнительный бенчмарк IFS-кэша против LRU-кэша на трафике Ципфа
        group.MapPost("/benchmark", async (
            [FromQuery] int requestsCount,
            [FromServices] IfsCachedRepository ifsRepo,
            [FromServices] LruCachedRepository lruRepo,
            [FromServices] PostgresRepository postgresRepo) =>
        {
            int count = Math.Clamp(requestsCount <= 0 ? 200 : requestsCount, 20, 2000);
            var catalog = (await postgresRepo.GetAll<Book>()).ToList();

            if (catalog.Count == 0)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Каталог пуст",
                    Detail = "Загрузите книги в БД перед запуском бенчмарка кэша."
                });
            }

            // Генерируем последовательность ключей по степенному закону Ципфа (Zipf)
            var targetKeys = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                int index = (int)(Math.Pow(Random.Shared.NextDouble(), 2.3) * catalog.Count);
                targetKeys.Add(catalog[Math.Clamp(index, 0, catalog.Count - 1)].Name);
            }

            // Замеряем IFS Cached Repository
            var ifsLatencies = await MeasureRepositoryAsync(ifsRepo, targetKeys);
            // Замеряем LRU Cached Repository
            var lruLatencies = await MeasureRepositoryAsync(lruRepo, targetKeys);

            double ifsTotal = ifsLatencies.Sum();
            double lruTotal = lruLatencies.Sum();

            return Results.Ok(new CacheBenchmarkResponse(
                TotalRequests: count,
                IfsTotalMs: Math.Round(ifsTotal, 2),
                IfsP50Ms: GetPercentile(ifsLatencies, 0.50),
                IfsP95Ms: GetPercentile(ifsLatencies, 0.95),
                LruTotalMs: Math.Round(lruTotal, 2),
                LruP50Ms: GetPercentile(lruLatencies, 0.50),
                LruP95Ms: GetPercentile(lruLatencies, 0.95),
                SpeedupFactor: ifsTotal > 0 ? Math.Round(lruTotal / ifsTotal, 2) : 1.0));
        })
        .WithSummary("Запустить сравнительный стресс-тест IFS vs LRU кэша по распределению Ципфа");

        return app;
    }

    private static async Task<List<double>> MeasureRepositoryAsync(IRepository repo, List<string> keys)
    {
        var latencies = new List<double>(keys.Count);
        var sw = new Stopwatch();

        foreach (var key in keys)
        {
            sw.Restart();
            _ = await repo.GetByName<Book>(key);
            sw.Stop();
            latencies.Add(sw.Elapsed.TotalMilliseconds);
        }

        latencies.Sort();
        return latencies;
    }

    private static double GetPercentile(List<double> sorted, double percentile)
    {
        if (sorted.Count == 0) return 0;
        int idx = (int)Math.Clamp(sorted.Count * percentile, 0, sorted.Count - 1);
        return Math.Round(sorted[idx], 4);
    }
}