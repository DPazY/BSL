using BSL.Models.Interface;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace BSL.App.Commands
{
    [Command("dump", Description = "Dump all books in file")]
    public class Dump(
        IXmlService xmlService,
        ILogger<Dump> logger,
        IConsole console)
    {
        [Argument(0, "outFile", Description = "File path to save the dump")]
        [Required(ErrorMessage = "Argument {0} is required")]
        public string OutFile { get; set; }

        public void OnExecute()
        {
            logger.LogDebug($"Starting dump in {OutFile}");

            using (Stream stream = new FileStream(OutFile, FileMode.Create))
            {
                xmlService.Export(stream);
            }

            logger.LogDebug("Dump is done");
            console.WriteLine("File successfully dumped into storage!");
        }

    }

}
