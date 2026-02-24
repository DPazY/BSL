namespace BSL.Models
{
    public interface IPatentService
    {
        IEnumerable<Patent> GetAll(OrderBy? orderBy = null);  
    }
}