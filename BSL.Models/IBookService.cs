namespace BSL.Models
{
    public interface IBookService
    {
        IEnumerable<Book> GetAll(OrderBy? orderByYear = null);
        IEnumerable<Book> GetAllByAuthor(string v);
        IEnumerable<Book> GetAllWherePublisherStarts(string pattern);
    }
}