using BSL.Models;
using System.Text.RegularExpressions;

namespace BSL.Implimentation
{
    public class BookService : IBookService
    {
        private readonly IRepository<Book> _bookRepository;

        public BookService(IRepository<Book> bookRepository)
        {
            _bookRepository = bookRepository;
        }

        public IEnumerable<Book> GetAll(OrderBy? orderBy = null)
        {
            return orderBy switch
            {
                OrderBy.Asc => _bookRepository.GetAll().OrderBy(b => b.YearBook),
                OrderBy.Desc => _bookRepository.GetAll().OrderByDescending(b => b.YearBook),
                _ => _bookRepository.GetAll()
            };
        }

        public IEnumerable<Book> GetAllByAuthor(string author)
        {
            if (!string.IsNullOrEmpty(author))
            {
                return _bookRepository.GetAll().Where(b => b.Author.Contains(author));
                
            }
            else return _bookRepository.GetAll();
        }

        public IEnumerable<Book> GetAllWherePublisherStarts(string pattern)
        {
            return _bookRepository.GetAll().Where(b => Regex.IsMatch(b.PublisherBook,
                $@"^{pattern}", RegexOptions.IgnoreCase)).OrderBy(b => b.PublisherBook);
        }

        public IEnumerable<Book> SearchByName(string name)
        {
            return _bookRepository.GetAll().Where(b => b.Name == name);
        }
    }
}
