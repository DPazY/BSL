using BSL.Models;
using BSL.Models.Enum;
using BSL.Models.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BSL.Implementation.Service
{
    public class BookService : EditionService, IBookService
    {
        public BookService(IRepository bookRepository) : base(bookRepository) { }

        public async Task<IEnumerable<Book>> GetAll(OrderBy? orderBy = null)
        {
            var books = await _editionRepository.GetAll<Book>();

            return orderBy switch
            {
                OrderBy.Asc => books.OrderBy(b => b.YearBook),
                OrderBy.Desc => books.OrderByDescending(b => b.YearBook),
                _ => books
            };
        }

        public async Task<IEnumerable<Book>> GetAllByAuthor(string author)
        {
            var books = await _editionRepository.GetAll<Book>();

            if (!string.IsNullOrEmpty(author))
            {
                return books.Where(b => b.Author.Contains(author));
            }

            return books;
        }

        public async Task<IEnumerable<Book>> GetAllWherePublisherStarts(string pattern)
        {
            var books = await _editionRepository.GetAll<Book>();

            return books
                .Where(b => b.PublisherBook.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
                .OrderBy(b => b.PublisherBook);
        }
    }
}