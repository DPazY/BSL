namespace BSL.Models
{
    public interface INewspaperService
    {
        public IEnumerable<Newspaper> GetAll(OrderBy? orderBy = null);
    }
    public interface IPatentService
    {
        public IEnumerable<Patent> GetAll(OrderBy? orderBy = null);
    }

}