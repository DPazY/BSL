using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;

namespace BSL.App.Commands
{
    [Command("upload", Description = "Upload all books")]
    public class Upload(
        ILogger<Upload> logger,
        IConsole console) // Убрали IXmlService, он здесь больше не нужен
    {
        [Argument(0, "inFile", Description = "File to upload")]
        [Required(ErrorMessage = "Argument {0} is required")]
        public string inFile { get; set; }

        // Поменяли сигнатуру на async Task OnExecuteAsync
        public async Task OnExecuteAsync()
        {
            logger.LogDebug($"Starting upload from {inFile}");

            using var client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:5000");

            using var multipartFormContent = new MultipartFormDataContent();

            using (Stream stream = new FileStream(inFile, FileMode.Open))
            {
                var fileStreamContent = new StreamContent(stream);
                fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue("application/xml");

                multipartFormContent.Add(fileStreamContent, name: "formFile", fileName: Path.GetFileName(inFile));

                logger.LogDebug("Sending file to server...");

                using var response = await client.PostAsync("/books/import", multipartFormContent);

                response.EnsureSuccessStatusCode();

                var responseText = await response.Content.ReadAsStringAsync();
                console.WriteLine($"Server response: {responseText}");
            }
        }
    }
}