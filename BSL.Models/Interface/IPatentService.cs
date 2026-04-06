using BSL.Models.Enum;

namespace BSL.Models.Interface
{
    public interface IPatentService
    {
        Task<IEnumerable<Patent>> GetAll(OrderBy? orderBy = null);  
    }
}