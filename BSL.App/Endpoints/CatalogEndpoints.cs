using System.Diagnostics;
using BSL.Implementation.Validator;
using BSL.Models;
using BSL.Models.Dto;
using BSL.Models.Interface;
using Microsoft.AspNetCore.Mvc;

namespace BSL.App.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalog")
            .WithTags("Catalog (Books, Newspapers, Patents)");

        // 1. Получение списка книг через твой IBookService (с фильтрами и сортировкой OrderBy)
        group.MapGet("/books", async (
            [AsParameters] BookFilterQuery query,
            [FromServices] IBookService bookService,
            HttpContext httpContext) =>
        {
            var sw = Stopwatch.StartNew();

            IEnumerable<Book> books;
            if (!string.IsNullOrWhiteSpace(query.Author))
            {
                books = await bookService.GetAllByAuthor(query.Author);
            }
            else if (!string.IsNullOrWhiteSpace(query.PublisherStartsWith))
            {
                books = await bookService.GetAllWherePublisherStarts(query.PublisherStartsWith);
            }
            else
            {
                books = await bookService.GetAll(query.OrderByYear);
            }

            var list = books.ToList();
            sw.Stop();

            AppendTelemetryHeaders(httpContext, sw.Elapsed.TotalMilliseconds);

            int page = Math.Max(1, query.Page);
            int pageSize = Math.Clamp(query.PageSize, 1, 100);
            var pagedItems = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return Results.Ok(new PagedResponse<Book>(pagedItems, page, pageSize, list.Count));
        })
        .WithSummary("Получить список книг (фильтрация по автору, издательству, сортировка по году)");

        // 2. Точечное получение книги по имени через IRepository.GetByName<Book> (горячий путь IFS-кэша)
        group.MapGet("/books/{name}", async (
            string name,
            [FromServices] IRepository repository,
            HttpContext httpContext) =>
        {
            var sw = Stopwatch.StartNew();
            var book = await repository.GetByName<Book>(name);
            sw.Stop();

            AppendTelemetryHeaders(httpContext, sw.Elapsed.TotalMilliseconds);

            return book is not null
                ? Results.Ok(book)
                : Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Книга не найдена",
                    Detail = $"Издание с именем '{name}' не найдено в каталоге."
                });
        })
        .WithSummary("Получить книгу по названию (проверяет IFS-кэш и обновляет веса телеметрии)");

        // 3. Добавление списка книг с проверкой через твой BookValidator
        group.MapPost("/books", async (
            [FromBody] List<Book> books,
            [FromServices] IRepository repository,
            [FromServices] BookValidator validator) =>
        {
            foreach (var book in books)
            {
                var validationResult = await validator.ValidateAsync(book);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }
            }

            await repository.Add<Book>(books);
            return Results.Created("/api/v1/catalog/books", new { AddedCount = books.Count });
        })
        .WithSummary("Добавить книги в каталог (с валидацией через BookValidator)");

        // 4. Удаление книг через IRepository.Remove<Book>
        group.MapDelete("/books", async (
            [FromBody] List<Book> books,
            [FromServices] IRepository repository) =>
        {
            await repository.Remove<Book>(books);
            return Results.Ok(new { RemovedCount = books.Count });
        })
        .WithSummary("Удалить переданные книги из каталога");

        // 5. Получение газет (раскрываем домен Newspaper)
        group.MapGet("/newspapers", async (
            [FromServices] IRepository repository,
            HttpContext httpContext) =>
        {
            var sw = Stopwatch.StartNew();
            var items = (await repository.GetAll<Newspaper>()).ToList();
            sw.Stop();

            AppendTelemetryHeaders(httpContext, sw.Elapsed.TotalMilliseconds);
            return Results.Ok(items);
        })
        .WithSummary("Получить все газеты из каталога");

        // 6. Получение патентов (раскрываем домен Patent)
        group.MapGet("/patents", async (
            [FromServices] IRepository repository,
            HttpContext httpContext) =>
        {
            var sw = Stopwatch.StartNew();
            var items = (await repository.GetAll<Patent>()).ToList();
            sw.Stop();

            AppendTelemetryHeaders(httpContext, sw.Elapsed.TotalMilliseconds);
            return Results.Ok(items);
        })
        .WithSummary("Получить все патенты из каталога");

        return app;
    }

    private static void AppendTelemetryHeaders(HttpContext context, double elapsedMs)
    {
        context.Response.Headers["X-Execution-Time-Ms"] = elapsedMs.ToString("F3");
        context.Response.Headers["X-Data-Source"] = elapsedMs < 2.0 ? "IFS-Cache" : "PostgreSQL";
    }
}