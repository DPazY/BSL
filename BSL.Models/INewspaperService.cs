namespace BSL.Models
{
    public interface INewspaperService
    {
        IEnumerable<Newspaper> GetAll(OrderBy? orderBy = null);
    }

}