using BSL.App;
using BSL.App.Service;
using BSL.Implementation;
using BSL.Implementation.Metrics;
using BSL.Implementation.Repository;
using BSL.Implementation.SerializerStrategy;
using BSL.Implementation.Service;
using BSL.Models;
using BSL.Models.Enum;
using BSL.Models.Interface;
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Hosting.WindowsServices;
using System.IO.Abstractions;

internal class Program
{
    async private static Task Main(string[] args)
    {
        var configurationBuilder = new ConfigurationBuilder();

        configurationBuilder
            .AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["WorkDir"] = Environment.CurrentDirectory,
                ["FileWatcher"] = Environment.CurrentDirectory,
                ["ProcessedFile"] = "None"
            })
            .AddUserSecrets<Program>();


        configurationBuilder.AddJsonFile("AppConfig.json");

        var configuration = configurationBuilder.Build();

        var workdir =
            configuration.GetSection("WorkDir").Value;
        var fileWatcher = configuration.GetSection("FileWatcher").Value;
        ProcessedFileAction processedFile = Enum.Parse<ProcessedFileAction>(configuration.GetSection("ProcessedFile").Value);

        var host = Host.CreateDefaultBuilder();

        host.ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/api/test/books/{name}", async (string name, IRepository repository) =>
                    {
                        return await repository.GetByName<Book>(name);
                    });
                });
            });
        });

        if (args.Length > 0)
            host.UseCommandLineApplication<App>(args);

        var hostBuilded = host
            .ConfigureServices(serviceCollection =>
            {
                serviceCollection.AddSingleton<IBookService, BookService>();
                serviceCollection.AddSingleton<IXmlService, BookXmlService>();
                serviceCollection.AddSingleton(new AppSettings(workdir, fileWatcher, processedFile));
                serviceCollection.AddSingleton<ISerializerStrategy, XmlSerializerStrategy>();
                serviceCollection.AddSingleton<IFileSystem>(provider =>
                {
                    return new FileSystem();
                });

                serviceCollection.AddMetrics();
                serviceCollection.AddSingleton<AppMetrics>();

                serviceCollection.AddSingleton<ITelemetryAggregator, TelemetryAggregator>();
                serviceCollection.AddSingleton<IIfsPredictor, IfsPredictor>();

                Dapper.SqlMapper.AddTypeHandler(new StringListTypeHandler());
                Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

                serviceCollection.AddSingleton<IRepository>(provider =>
                {
                    string connectionString = configuration["ConnectionString"];

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


                if (args.Length == 0)
                {
                    serviceCollection.AddHostedService<IfsBackgroundPrefetcher>();
                    serviceCollection.AddHostedService<FileWatcher>();
                    serviceCollection.AddHostedService<BookProcessor>();
                    serviceCollection.AddSingleton<FileProcessingQueue>();

                    if (WindowsServiceHelpers.IsWindowsService())
                        serviceCollection.AddWindowsService();
                    else if (SystemdHelpers.IsSystemdService())
                        serviceCollection.AddSystemd();
                }
            })
            .ConfigureLogging(loggingBuilder =>
            {
                loggingBuilder.AddConsole();
                loggingBuilder.SetMinimumLevel(LogLevel.Information);
            })
            .Build();

        if (args.Length > 0)
        {
            await hostBuilded.RunCommandLineApplicationAsync();
            await hostBuilded.RunAsync();
        }
        else
        {
            using var meterProvider = MetricPublisihingHelpers
                .GetMeterProvider(AppMetrics.MeterName, "http://localhost:9184/");

            await hostBuilded.RunAsync();
        }
    }
}