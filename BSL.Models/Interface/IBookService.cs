using BSL.Models.Enum;

namespace BSL.Models.Interface
{
    public interface IBookService
    {
        IEnumerable<Book> GetAll(OrderBy? orderByYear = null);
        IEnumerable<Book> GetAllByAuthor(string v);
        IEnumerable<Book> GetAllWherePublisherStarts(string pattern);
    }
}