using System;
using System.Collections.Generic;

namespace BSL.Models
{
    public interface IBookService
    {
        public IEnumerable<Book> GetAll(OrderBy? orderByYear = null);
        IEnumerable<Book> GetAllByAuthor(string author);
        IEnumerable<Book> GetAllWherePublisherStarts(string pattern);
        bool Add(Book book);
        bool Remove(Book book);
        IEnumerable<Book> SearchByName(string name);
    }
}