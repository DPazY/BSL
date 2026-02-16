namespace BSL.Models
{
    public interface IPatentService
    {
        public IEnumerable<Patent> GetAll(OrderBy? orderBy = null);
        IEnumerable<Patent> SearchByName(string name);
    }

}