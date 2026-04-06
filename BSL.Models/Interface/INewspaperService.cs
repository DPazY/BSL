using BSL.Models.Enum;

namespace BSL.Models.Interface
{
    public interface INewspaperService
    {
        Task<IEnumerable<Newspaper>> GetAll(OrderBy? orderBy = null);
    }

}