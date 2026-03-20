namespace BSL.Models.Interface
{

    public interface IRepository
    {
        IEnumerable<T> GetAll<T>() where T : Edition;
        void Add<T>(IEnumerable<T> editions) where T : Edition;
        void Remove<T>(IEnumerable<T> editions) where T : Edition;
        public T? GetByName<T>(string name) where T : Edition;
    }
}
