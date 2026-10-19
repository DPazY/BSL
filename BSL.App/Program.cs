using System.IO.Abstractions;
using BSL.App.Endpoints;
using BSL.App.Infrastructure;
using BSL.App.Service;
using BSL.Implementation;
using BSL.Implementation.Metrics;
using BSL.Implementation.Repository;
using BSL.Implementation.SerializerStrategy;
using BSL.Implementation.Service;
using BSL.Implementation.Validator;
using BSL.Models;
using BSL.Models.Enum;
using BSL.Models.Interface;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Hosting.WindowsServices;

namespace BSL.App;

public class Program
{
    public static async Task Main(string[] args)
    {
        bool isWebMode = args.Contains("--web");
        string[] appArgs = args.Where(a => a != "--web").ToArray();

        // 1. Собираем конфигурацию с безопасными дефолтами
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WorkDir"] = Environment.CurrentDirectory,
                ["FileWatcher"] = Environment.CurrentDirectory,
                ["ProcessedFile"] = "None"
            })
            .AddUserSecrets<Program>()
            .AddJsonFile("AppConfig.json", optional: true, reloadOnChange: true)
            .Build();

        string workdir = configuration.GetSection("WorkDir").Value ?? Environment.CurrentDirectory;
        string fileWatcher = configuration.GetSection("FileWatcher").Value ?? Environment.CurrentDirectory;
        string processedFileRaw = configuration.GetSection("ProcessedFile").Value ?? "None";
        ProcessedFileAction processedFile = Enum.TryParse<ProcessedFileAction>(processedFileRaw, true, out var parsedAction)
            ? parsedAction
            : ProcessedFileAction.None;

        // Регистрируем Dapper TypeHandlers
        Dapper.SqlMapper.AddTypeHandler(new StringListTypeHandler());
        Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

        // СЦЕНАРИЙ А: Если переданы аргументы командной строки -> запускаем CLI режим через McMaster DI
        if (appArgs.Length > 0)
        {
            var services = new ServiceCollection();
            ConfigureCoreServices(services, configuration, workdir, fileWatcher, processedFile, isWebMode);
            services.AddLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });

            await using var serviceProvider = services.BuildServiceProvider();

            var appCli = new CommandLineApplication<App>();
            appCli.Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(serviceProvider);

            await appCli.ExecuteAsync(appArgs);
            return;
        }

        // СЦЕНАРИЙ Б: Запуск полноценного Web API хоста + фоновых служб
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddConfiguration(configuration);
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        // Регистрируем общие доменные сервисы, кэши и репозитории
        ConfigureCoreServices(builder.Services, configuration, workdir, fileWatcher, processedFile, isWebMode);

        // Регистрируем фоновые воркеры
        builder.Services.AddSingleton<FileProcessingQueue>();
        builder.Services.AddHostedService<IfsBackgroundPrefetcher>();
        builder.Services.AddHostedService<FileWatcher>();
        builder.Services.AddHostedService<BookProcessor>();

        if (WindowsServiceHelpers.IsWindowsService())
            builder.Services.AddWindowsService();
        else if (SystemdHelpers.IsSystemdService())
            builder.Services.AddSystemd();

        // Инфраструктура Web API: Swagger, ProblemDetails и CORS для React UI (bsl-ui)
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .WithExposedHeaders("X-Execution-Time-Ms", "X-Data-Source"));
        });

        var app = builder.Build();

        app.UseExceptionHandler();
        app.UseCors("AllowFrontend");

        app.UseSwagger();
        app.UseSwaggerUI();

        // Health-check эндпоинт
        app.MapGet("/api/health", () => Results.Ok(new { Status = "Healthy", Mode = "Web API" }))
           .WithTags("System");

        app.MapCatalogEndpoints();
        app.MapCacheEndpoints();
        app.MapIngestionEndpoints();

        using var meterProvider = MetricPublishingHelpers
            .GetMeterProvider(AppMetrics.MeterName, "http://localhost:9184/");

        await app.RunAsync();
    }

    private static void ConfigureCoreServices(
        IServiceCollection services,
        IConfiguration configuration,
        string workdir,
        string fileWatcher,
        ProcessedFileAction processedFile,
        bool isWebMode)
    {
        services.AddSingleton<IBookService, BookService>();
        services.AddSingleton<INewspaperService, NewspaperService>();
        services.AddSingleton<IPatentService, PatentService>();
        services.AddSingleton<IEditionService, EditionService>();

        services.AddSingleton<BookValidator>();
        services.AddSingleton<NewspaperValidator>();
        services.AddSingleton<PatentValidator>();
        services.AddSingleton<EditionValidator>();

        if (isWebMode)
        {
            services.AddHttpClient("BslWebClient", client =>
            {
                client.BaseAddress = new Uri("http://localhost:5155");
                client.DefaultRequestHeaders.Add("X-API-Key", "b3c29924-814d-4876-80db-f9540b6e92b5");
            });

            services.AddSingleton<IXmlService, WebXmlService>();
            Console.WriteLine("[System] Запуск в WEB режиме...");
        }
        else
        {
            services.AddSingleton<IXmlService, BookXmlService>();
            Console.WriteLine("[System] Запуск в ЛОКАЛЬНОМ режиме...");
        }

        services.AddSingleton(new AppSettings(workdir, fileWatcher, processedFile));

        services.AddSingleton<ISerializerStrategy, XmlSerializerStrategy>();
        services.AddSingleton<XmlSerializerStrategy>();
        services.AddSingleton<JsonSerializerStrategy>();
        services.AddSingleton<ProtobufSerializerStrategy>();

        services.AddSingleton<IFileSystem>(_ => new FileSystem());

        services.AddMetrics();
        services.AddSingleton<AppMetrics>();
        services.AddSingleton<ITelemetryAggregator, TelemetryAggregator>();
        services.AddSingleton<IIfsPredictor, IfsPredictor>();

        services.AddSingleton<PostgresRepository>(_ =>
        {
            string connectionString = configuration["ConnectionString"] ?? string.Empty;
            return new PostgresRepository(connectionString);
        });

        services.AddSingleton<LruCachedRepository>(provider =>
        {
            var postgres = provider.GetRequiredService<PostgresRepository>();
            return new LruCachedRepository(postgres, 100);
        });

        services.AddSingleton<IfsCachedRepository>(provider =>
        {
            var postgres = provider.GetRequiredService<PostgresRepository>();
            var telemetryAggregator = provider.GetRequiredService<ITelemetryAggregator>();
            long cacheMemoryLimitBytes = 100 * 1024;
            return new IfsCachedRepository(postgres, telemetryAggregator, cacheMemoryLimitBytes);
        });

        services.AddSingleton<IRepository>(provider =>
        {
            var ifsCachedRepository = provider.GetRequiredService<IfsCachedRepository>();
            var appMetrics = provider.GetRequiredService<AppMetrics>();
            return new MetricsRepository(ifsCachedRepository, appMetrics);
        });
    }
}