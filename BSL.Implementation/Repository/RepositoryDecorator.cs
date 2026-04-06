using BSL.Models;
using BSL.Models.Interface;

namespace BSL.Implementation.Repository
{
    public abstract class RepositoryDecorator : IRepository
    {
        protected readonly IRepository _repository;

        protected RepositoryDecorator(IRepository repository)
        {
            _repository = repository;
        }

        public virtual async Task Add<T>(IEnumerable<T> editions) where T : Edition 
        {
            await _repository.Add(editions);
        }

        public virtual async Task<IEnumerable<T>> GetAll<T>() where T : Edition
        {
            return await _repository.GetAll<T>();
        }

        public virtual async Task<T> GetByName<T>(string name) where T : Edition
        { 
            return await _repository.GetByName<T>(name);
        }

        public virtual async Task Remove<T>(IEnumerable<T> editions) where T : Edition
        {
            await _repository.Remove(editions);
        }
    }
}
