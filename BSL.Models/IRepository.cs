namespace BSL.Models
{

    public interface IRepository<T>
    {
        IEnumerable<T> GetAll();
    }
}
