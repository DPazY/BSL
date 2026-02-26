using BSL.Models;
using System.Xml.Serialization;

namespace BSL.Implementation
{
    public class BookXmlService : IXmlService
    {
        private IRepository _bookXmlRepository;
        public BookXmlService(IRepository bookXmlRepozitory)
        {
            _bookXmlRepository = bookXmlRepozitory;
        }

        public void Import(Stream stream)
        {
            var serializer = new XmlSerializer(typeof(CatalogXmlDto));
            var catalog = (CatalogXmlDto?)serializer.Deserialize(stream);

            if (catalog?.Books == null) return;

            var newBooksForRepository = new List<Book>();
            var booksForRepository = _bookXmlRepository.GetAll<Book>().ToList();

            var hashset = new HashSet<string>(booksForRepository.Select(b => b.Name));
           
                foreach (var book in catalog.Books)
                {
                    string name = book.Title ?? "Неизвестное название";
                    string author = book.Author ?? "Неизвестный автор";
                    string publisher = book.Publisher ?? "Неизвестное издательство";

                    DateOnly publishDate = DateOnly.MinValue;
                    if (DateOnly.TryParse(book.PublishDate, out var parsedDate))
                    {
                        publishDate = parsedDate;
                    }
                    else
                    {
                        publishDate = new DateOnly(1900, 1, 1);
                    }
                    if (!hashset.Contains(name))
                    {
                        hashset.Add(name);
                        var domainBook = new Book(name, publishDate, publisher, author);
                        newBooksForRepository.Add(domainBook);
                    }
                }

            if (newBooksForRepository.Any())
            {
                booksForRepository.AddRange(newBooksForRepository);

                _bookXmlRepository.Add(booksForRepository);
            }
        }
    }
}
