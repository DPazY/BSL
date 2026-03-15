using BSL.Models.Interface;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace BSL.App.Commands
{
    [Command("upload", Description = "Upload all books")]
    public class Upload(
        IXmlService xmlService,
        ILogger<Upload> logger,
        IConsole console)
    {
        [Argument(0, "inFile", Description = "File to upload")]
        [Required(ErrorMessage = "Argument {0} is required")]
        public string inFile { get; set; }

        public void OnExecute()
        {
            logger.LogDebug($"Starting upload from {inFile}");

            using (Stream stream = new FileStream(inFile, FileMode.Open))
            {
                xmlService.Import(stream);
            }

            logger.LogDebug("Upload is done");
            console.WriteLine("File successfully uploaded into storage!");
        }
    }
}
