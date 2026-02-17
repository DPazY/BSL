namespace BSL.Models
{
    public interface IPatentService
    {
        public IEnumerable<Patent> GetAll(OrderBy? orderBy = null);  
    }
}