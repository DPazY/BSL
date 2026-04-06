using BSL.Models.Enum;

namespace BSL.Models.Interface
{
    public interface IBookService
    {
        Task<IEnumerable<Book>> GetAll(OrderBy? orderByYear = null);
        Task<IEnumerable<Book>> GetAllByAuthor(string v);
        Task<IEnumerable<Book>> GetAllWherePublisherStarts(string pattern);
    }
}