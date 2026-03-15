using BSL.App;
using BSL.App.Service;
using BSL.Implementation.Repository;
using BSL.Implementation.Service;
using BSL.Models.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal class Program
{
    async private static Task Main(string[] args)
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

        await Host.CreateDefaultBuilder()
                .UseCommandLineApplication<App>(args)
                .ConfigureServices(serviceCollection =>
                {
                    serviceCollection.AddSingleton<IBookService, BookService>();
                    serviceCollection.AddSingleton<IXmlService, BookXmlService>();
                    serviceCollection.AddSingleton<IRepository, CachedRepository>();
                    serviceCollection.AddSingleton<AppSettings>(new AppSettings(workdir));
                })
                .ConfigureLogging(loggingBuilder =>
                {
                    loggingBuilder.AddConsole();
                    loggingBuilder.SetMinimumLevel(LogLevel.Debug);
                })
                .Build()
                .RunCommandLineApplicationAsync();
    }
}