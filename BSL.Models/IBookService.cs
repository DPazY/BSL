namespace BSL.Models
{
    public interface IBookService
    {
        public IEnumerable<Book> GetAll(OrderBy? orderByYear = null);
        IEnumerable<Book> GetAllWherePublisherStarts(string pattern);
    }
}