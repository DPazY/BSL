namespace BSL.Models
{
    public interface INewspaperService
    {
        public IEnumerable<Newspaper> GetAll(OrderBy? orderBy = null);
        IEnumerable<Newspaper> SearchByName(string name);
    }

}