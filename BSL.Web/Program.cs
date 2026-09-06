using BSL.App.Commands;
using BSL.App.Service;
using BSL.Implementation;
using BSL.Implementation.Metrics;
using BSL.Implementation.Repository;
using BSL.Implementation.SerializerStrategy;
using BSL.Implementation.Service;
using BSL.Models;
using BSL.Models.Enum;
using BSL.Models.Interface;
using OpenTelemetry.Metrics;
using Scalar.AspNetCore;
using System.IO.Abstractions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["WorkDir"] = Environment.CurrentDirectory,
        ["ProcessedFile"] = "None"
    })
    .AddJsonFile("AppConfig.json", optional: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables();

var workdir = builder.Configuration.GetRequiredSection("WorkDir").Value;
var fileWatcher = builder.Configuration.GetValue<string>("FileWatcher") ?? Environment.CurrentDirectory;
var processedFile = builder.Configuration.GetValue<ProcessedFileAction>("ProcessedFile");

builder.Services.AddControllers();
builder.Services.AddSingleton<IBookService, BookService>();
builder.Services.AddSingleton<IXmlService, BookXmlService>();
builder.Services.AddSingleton(new AppSettings(workdir, fileWatcher, processedFile));
builder.Services.AddSingleton<ISerializerStrategy, XmlSerializerStrategy>();
builder.Services.AddSingleton<IFileSystem>(_ => new FileSystem());

builder.Services.AddMetrics();
builder.Services.AddSingleton<AppMetrics>();

builder.Services.AddSingleton<ITelemetryAggregator, TelemetryAggregator>();
builder.Services.AddSingleton<IIfsPredictor, IfsPredictor>();

Dapper.SqlMapper.AddTypeHandler(new StringListTypeHandler());
Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

builder.Services.AddSingleton<IRepository>(provider =>
{
    string connectionString = builder
    .Configuration.GetConnectionString("DefaultConnection")
        ?? builder.Configuration["ConnectionString"]
        ?? string.Empty;

    var appMetrics = provider.GetRequiredService<AppMetrics>();
    var telemetryAggregator = provider.GetRequiredService<ITelemetryAggregator>();

    var postgresRepository = new PostgresRepository(connectionString);

    long cacheMemoryLimitBytes = 100 * 1024;

    var cachedRepository = new CachedRepository(postgresRepository);
    var lruCachedRepository = new LruCachedRepository(postgresRepository, 100);

    var ifsCachedRepository = new IfsCachedRepository(
        postgresRepository,
        telemetryAggregator,
        cacheMemoryLimitBytes);

    //var metricsRepository = new MetricsRepository(ifsCachedRepository, appMetrics);
    var metricsRepository = new MetricsRepository(lruCachedRepository, appMetrics);

    return metricsRepository;
});

builder.Services.AddHostedService<IfsBackgroundPrefetcher>();

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter(AppMetrics.MeterName);
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddHttpClientInstrumentation();
        metrics.AddPrometheusExporter();
        metrics.AddConsoleExporter();
    });

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "BSL — High-Performance Caching API",
            Version = "v1",
            Description = "API для работы с каталогом публикаций и стресс-тестирования предиктивного кэша."
        };
        return Task.CompletedTask;
    });
});

builder.Logging.ClearProviders();
builder.Logging.AddSystemdConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

if (!app.Environment.IsDevelopment() && app.Environment.EnvironmentName != "Docker")
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

//app.UseApiKey("api_key.json");

app.MapControllers();

app.MapPost("/books/import", async (
        IXmlService xmlService,
        ILogger<Upload> logger,
        IFormFile formFile) =>
{
    if (formFile == null || formFile.Length == 0)
    {
        return Results.BadRequest("Файл не выбран или пуст");
    }

    logger.LogDebug("Starting upload from {FileName}", formFile.FileName);

    using (Stream stream = formFile.OpenReadStream())
    {
        await xmlService.Import(stream);
    }

    logger.LogDebug("Upload is done");
    return Results.Ok("Файл успешно загружен!");
})
.WithName("ImportBooks")
.WithTags("Books")
.DisableAntiforgery();

app.MapGet("/dumps/full", async (
    IXmlService xmlService,
    ILogger<Upload> logger) =>
{
    var file = new MemoryStream();
    await xmlService.Export(file);
    file.Position = 0;

    logger.LogDebug("Dump is done");
    return Results.Stream(file, "application/xml", "books_dump.xml");
})
.WithName("ExportFullDump")
.WithTags("Dumps");

app.MapGet("/books/{name}", async (string name, IRepository repository) =>
{
    var res = await repository.GetByName<Book>(name);
    if (res == null)
    {
        return Results.NotFound("Книга с таким именем не найдена");
    }

    return Results.Ok(res);
})
.WithName("GetBookByName")
.WithTags("Books");

app.MapPrometheusScrapingEndpoint();

app.Run();