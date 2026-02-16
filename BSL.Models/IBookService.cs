namespace BSL.Models
{
    public interface IBookService
    {
        public IEnumerable<Book> GetAll(OrderBy? orderByYear = null);
        IEnumerable<Book> GetAllByAuthor(string v);
        IEnumerable<Book> GetAllWherePublisherStarts(string pattern);
        IEnumerable<Book> SearchByName(string name);
    }
}