using BSL.Models.Enum;

namespace BSL.Models.Interface
{
    public interface IPatentService
    {
        IEnumerable<Patent> GetAll(OrderBy? orderBy = null);  
    }
}