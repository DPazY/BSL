using BSL.App;
using BSL.App.Service;
using BSL.Implementation;
using BSL.Implementation.Repository;
using BSL.Implementation.SerializerStrategy;
using BSL.Implementation.Service;
using BSL.Models;
using BSL.Models.Enum;
using BSL.Models.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
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
        if (args.Length > 0)
            host.UseCommandLineApplication<App>(args);

        var hostBuilded = host
            .ConfigureServices(serviceCollection =>
            {
                serviceCollection.AddSingleton<IBookService, BookService>();
                serviceCollection.AddSingleton<IXmlService, BookXmlService>();
                serviceCollection.AddSingleton<AppSettings>(new AppSettings(workdir, fileWatcher, processedFile));
                serviceCollection.AddSingleton<ISerializerStrategy, XmlSerializerStrategy>();
                serviceCollection.AddSingleton<IFileSystem>(provider =>
                {
                    return new FileSystem();
                });
                Dapper.SqlMapper.AddTypeHandler(new StringListTypeHandler());
                Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

                serviceCollection.AddSingleton<IRepository>(provider =>
                {
                    string connectionString = configuration["ConnectionString"];

                    var postgresRepository = new PostgresRepository(connectionString);

                    return new CachedRepository(postgresRepository);
                });

                if (args.Length == 0)
                {
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
                loggingBuilder.SetMinimumLevel(LogLevel.Debug);
            })
            .Build();

        if (args.Length > 0) await hostBuilded.RunCommandLineApplicationAsync();
        else await hostBuilded.RunAsync();
    }
}