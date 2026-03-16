using BSL.Models;
using BSL.Models.Enum;
using BSL.Models.Interface;

namespace BSL.Implementation.Service
{
    public class BookService : EditionService, IBookService
    {
        public BookService(IRepository bookRepository) : base(bookRepository) { }


        public IEnumerable<Book> GetAll(OrderBy? orderBy = null)
        {
            return orderBy switch
            {
                OrderBy.Asc => _editionRepository.GetAll<Book>().OrderBy(b => b.YearBook),
                OrderBy.Desc => _editionRepository.GetAll<Book>().OrderByDescending(b => b.YearBook),
                _ => _editionRepository.GetAll<Book>()
            };
        }


        public IEnumerable<Book> GetAllByAuthor(string author)
        {
            if (!string.IsNullOrEmpty(author))
            {
                return _editionRepository.GetAll<Book>().Where(b => b.Author.Contains(author));

            }
            else return _editionRepository.GetAll<Book>();
        }

        public IEnumerable<Book> GetAllWherePublisherStarts(string pattern)
        {
            return _editionRepository.GetAll<Book>()
                .Where(b => b.PublisherBook.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
                .OrderBy(b => b.PublisherBook);
        }
    }
}
