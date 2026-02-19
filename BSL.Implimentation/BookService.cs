using BSL.Models;
using System.Text.RegularExpressions;

namespace BSL.Implimentation
{
    public class BookService : IBookService
    {
        private readonly IRepository _bookRepository;

        public BookService(IRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        public IEnumerable<Book> GetAll(OrderBy? orderBy = null)
        {
            return orderBy switch
            {
                OrderBy.Asc => _bookRepository.GetAll<Book>().OrderBy(b => b.YearBook),
                OrderBy.Desc => _bookRepository.GetAll<Book>().OrderByDescending(b => b.YearBook),
                _ => _bookRepository.GetAll<Book>()
            };
        }

        public IEnumerable<Book> GetAllByAuthor(string author)
        {
            if (!string.IsNullOrEmpty(author))
            {
                return _bookRepository.GetAll<Book>().Where(b => b.Author.Contains(author));
                
            }
            else return _bookRepository.GetAll<Book>();
        }

        public IEnumerable<Book> GetAllWherePublisherStarts(string pattern)
        {
            return _bookRepository.GetAll<Book>().Where(b => Regex.IsMatch(b.PublisherBook,
                $@"^{pattern}", RegexOptions.IgnoreCase)).OrderBy(b => b.PublisherBook);
        }
    }
}
