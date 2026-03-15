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

        public virtual void Add<T>(IEnumerable<T> editions)
        {
            _repository.Add(editions);
        }

        public virtual IEnumerable<T> GetAll<T>()
        {
            return _repository.GetAll<T>();
        }

        public virtual void Remove<T>(IEnumerable<T> editions)
        {
            _repository.Remove(editions);
        }
    }
}
