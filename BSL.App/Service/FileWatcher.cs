using BSL.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BSL.App.Service
{
    internal class FileWatcher(
        FileProcessingQueue queue,
        ILogger<FileWatcher> logger,
        AppSettings appSettings)
        : IHostedService
    {
        private readonly FileSystemWatcher watcher = new FileSystemWatcher();
        public Task StartAsync(CancellationToken cancellationToken)
        {
            var inDirectory = appSettings.FileWatcherDirectory;

            if (!Directory.Exists(inDirectory))
                Directory.CreateDirectory(inDirectory);

            watcher.Path = inDirectory;
            logger.LogInformation($"Начинаю слушать папку: {watcher.Path}"); 
            watcher.Filter = "*.*";

            watcher.Created += FileCreated;
            watcher.EnableRaisingEvents = true;
            return Task.CompletedTask;
        }

        private void FileCreated(object sender, FileSystemEventArgs e)
        {
            logger.LogInformation($"Обнаружен новый файл: {e.FullPath}");
            queue.Add(new FileProcessingItem(e.FullPath));
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            watcher.EnableRaisingEvents = false;
            return Task.CompletedTask;
        }
    }
}
