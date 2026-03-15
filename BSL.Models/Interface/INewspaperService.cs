using BSL.Models.Enum;

namespace BSL.Models.Interface
{
    public interface INewspaperService
    {
        IEnumerable<Newspaper> GetAll(OrderBy? orderBy = null);
    }

}