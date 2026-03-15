namespace BSL.Models.Interface
{

    public interface IRepository
    {
        IEnumerable<T> GetAll<T>();
        void Add<T>(IEnumerable<T> editions);
        void Remove<T>(IEnumerable<T> editions);

    }
}
