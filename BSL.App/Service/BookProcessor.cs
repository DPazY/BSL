using BSL.Models;
using BSL.Models.Enum;
using BSL.Models.Interface;
using Microsoft.Extensions.Hosting;

namespace BSL.App.Service
{
    public class BookProcessor(
        AppSettings appSettings,
        FileProcessingQueue queue,
        IXmlService xmlService) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var item = await queue.Get(stoppingToken);
                string newFilePath = item.FilePath;
                try
                {
                    using (var file = new FileStream(newFilePath, FileMode.Open))
                    {
                        xmlService.Import(file);
                    }

                    switch (appSettings.ProcessedFileAction)
                    {
                        case ProcessedFileAction.Delete : 
                            File.Delete(newFilePath);
                            break;
                        case ProcessedFileAction.None : 
                            break;
                    }
                    
                }
                catch (IOException ex)
                {
                    await queue.Add(new FileProcessingItem(newFilePath));
                    await Task.Delay(1000);
                }
            }
        }
    }
}
