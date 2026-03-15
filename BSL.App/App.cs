using BSL.App.Commands;
using BSL.App.Service;
using BSL.Implementation.Repository;
using BSL.Implementation.SerializerStrategy;
using BSL.Implementation.Service;
using BSL.Models.Interface;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Hosting.WindowsServices;
using System.IO.Abstractions;

namespace BSL.App
{
    public record AppSettings(string WorkDirectory);

    [HelpOption("-?|-h|--help")]
    [Subcommand(typeof(List))]
    public class App
    {
        async public Task OnExecute(CancellationToken cancellationToken)
        {
            var configurationBuilder = new ConfigurationBuilder();

            configurationBuilder.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["WorkDir"] = Environment.CurrentDirectory
                });


            configurationBuilder.AddJsonFile("AppConfig.json", true);

            var configuration = configurationBuilder.Build();

            var workdir =
                configuration.GetSection("WorkDir").Value;

            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddHostedService<FileWatcher>();
            builder.Services.AddHostedService<BookProcessor>();
            builder.Services.AddSingleton<FileProcessingQueue>();
            builder.Services.AddSingleton<ISerializerStrategy, XmlSerializerStrategy>();
            builder.Services.AddSingleton<IXmlService, BookXmlService>();
            builder.Services.AddSingleton(new AppSettings(workdir));
            builder.Services.AddSingleton<IFileSystem>(provider =>
            {
                return new FileSystem();
            });
            builder.Services.AddSingleton<IRepository>(provider =>
            {
                var settings = provider.GetRequiredService<AppSettings>();
                var fileSystem = provider.GetRequiredService<IFileSystem>();
                var serializer = provider.GetRequiredService<ISerializerStrategy>();

                var repository = new FileRepository(fileSystem, settings.WorkDirectory, serializer);

                return new CachedRepository(repository);
            });

            if (WindowsServiceHelpers.IsWindowsService())
                builder.Services.AddWindowsService();
            else if (SystemdHelpers.IsSystemdService())
                builder.Services.AddSystemd();

            var host = builder.Build();
            await host.RunAsync(cancellationToken);

        }
    }
}
