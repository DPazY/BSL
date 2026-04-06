using BSL.Implementation.Service;
using BSL.Models.Interface;
using McMaster.Extensions.CommandLineUtils;
using System.ComponentModel.DataAnnotations;

namespace BSL.App.Commands
{
    [Command("list", Description = "Show list")]
    public class List(
        BookService bookService,
        IXmlService xmlService,
        ILogger<Upload> logger,
        IConsole console)
    {
        [Required]
        [Argument(0, "outFile", Description = "Path to save the filtered dump")]
        public string OutFile { get; set; }

        [Option("-a|--author", Description = "Search books by author")]
        public string Author { get; set; }

        [Option("-p|--publisher", Description = "Search books by publisher")]
        public string Publisher { get; set; }

        [Option("-t|--name", Description = "Search books by name")]
        public string Name { get; set; }

        public async Task OnExecute()
        {
            using (Stream stream = new FileStream(OutFile, FileMode.Create))
            {

                if (Name != null)
                {
                    await xmlService.Export(stream, (await bookService
                        .GetAll())
                        .Where(e => e.Name == Name));
                }
                else if (Publisher != null)
                {
                    await xmlService.Export(stream, await bookService.GetAllWherePublisherStarts(Publisher));

                }
                else if (Author != null)
                {
                    await xmlService.Export(stream, await bookService.GetAllByAuthor(Author));

                }
                else
                {
                    await xmlService.Export(stream, await bookService.GetAll());
                }
            }
        }
    }
}
