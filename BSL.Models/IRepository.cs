namespace BSL.Models
{

    public interface IRepository
    {
        IEnumerable<T> GetAll<T>();
        void Add<T>(IEnumerable<T> editons);
        void Remove<T>(IEnumerable<T> editons);

    }
}
