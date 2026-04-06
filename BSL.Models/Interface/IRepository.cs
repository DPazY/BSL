namespace BSL.Models.Interface
{

    public interface IRepository
    {
        Task<IEnumerable<T>> GetAll<T>() where T : Edition;
        Task Add<T>(IEnumerable<T> editions) where T : Edition;
        Task Remove<T>(IEnumerable<T> editions) where T : Edition;
        Task<T> GetByName<T>(string name) where T : Edition;
    }
}
