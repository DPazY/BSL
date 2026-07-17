using BSL.App.Commands;
using BSL.App.Service;
using BSL.Implementation;
using BSL.Implementation.Metrics;
using BSL.Implementation.Repository;
using BSL.Implementation.SerializerStrategy;
using BSL.Implementation.Service;
using BSL.Models;
using BSL.Security;
using BSL.Models.Enum;
using BSL.Models.Interface;
using OpenTelemetry.Metrics;
using System.IO.Abstractions;

var builder = WebApplication.CreateBuilder(args);


builder.Configuration
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["WorkDir"] = Environment.CurrentDirectory,
        ["ProcessedFile"] = "None"
    })
    .AddJsonFile("AppConfig.json", optional: true)
    .AddUserSecrets<Program>();


var workdir =
    builder.Configuration.GetRequiredSection("WorkDir").Value;
var fileWatcher = builder.Configuration.GetValue<string>("FileWatcher") ?? Environment.CurrentDirectory;
var processedFile = builder.Configuration.GetValue<ProcessedFileAction>("ProcessedFile");

builder.Services.AddControllers();

builder.Services.AddSingleton<IBookService, BookService>();
builder.Services.AddSingleton<IXmlService, BookXmlService>();
builder.Services.AddSingleton(new AppSettings(
    workdir, fileWatcher, processedFile));
builder.Services.AddSingleton<ISerializerStrategy, XmlSerializerStrategy>();
builder.Services.AddSingleton<IFileSystem>(provider =>
{
    return new FileSystem();
});

builder.Services.AddMetrics();
builder.Services.AddSingleton<AppMetrics>();

builder.Services.AddSingleton<ITelemetryAggregator, TelemetryAggregator>();
builder.Services.AddSingleton<IIfsPredictor, IfsPredictor>();

Dapper.SqlMapper.AddTypeHandler(new StringListTypeHandler());
Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

builder.Services.AddSingleton<IRepository>(provider =>
{
    string connectionString = builder.Configuration["ConnectionString"];

    var appMetrics = provider.GetRequiredService<AppMetrics>();
    var telemetryAggregator = provider.GetRequiredService<ITelemetryAggregator>();

    var postgresRepository = new PostgresRepository(connectionString);

    var cachedRepository = new CachedRepository(postgresRepository);
    var lruCachedRepository = new LruCachedRepository(postgresRepository, 100);

    long cacheMemoryLimitBytes = 100 * 1024;

    var ifsCachedRepository = new IfsCachedRepository(
        postgresRepository,
        telemetryAggregator,
        cacheMemoryLimitBytes);
    var metricsRepository = new MetricsRepository(ifsCachedRepository, appMetrics);
    //var metricsRepository = new MetricsRepository(lruCachedRepository, appMetrics);

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
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Logging.ClearProviders();
builder.Logging.AddSystemdConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.UseApiKey("api_key.json");

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

    logger.LogDebug($"Starting upload from {formFile.FileName}");

    using (Stream stream = formFile.OpenReadStream())
    {
        await xmlService.Import(stream);
    }

    logger.LogDebug("Upload is done");

    return Results.Ok("Файл успешно загружен!");
});
app.MapGet("/dumps/full", async (
    IXmlService xmlService,
    ILogger<Upload> logger
    ) =>
{
    var file = new MemoryStream();
    await xmlService.Export(file);

    file.Position = 0;

    logger.LogDebug("Dump is done");
    return Results.Stream(file, "application/xml", "books_dump.xml");
});
app.MapGet("/books/{name}", async (string name, IRepository repository) =>
{
    var res = await repository.GetByName<Book>(name);

    if (res == null)
    {
        return Results.NotFound("Книга с таким именем не найдена");
    }

    return Results.Ok(res);
});

app.MapPrometheusScrapingEndpoint();

app.Run();
