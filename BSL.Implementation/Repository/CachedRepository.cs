using BSL.Implementation.Metrics;
using BSL.Models;
using BSL.Models.Interface;
using System.Collections.Concurrent;

namespace BSL.Implementation.Repository
{
    public class CachedRepository : RepositoryDecorator
    {
        private readonly object _locker = new();
        private readonly ConcurrentDictionary<Type, object> _dictRepository = new();

        public CachedRepository(IRepository innerRepository) : base(innerRepository)
        {
        }

        public override void Add<T>(IEnumerable<T> editions)
        {
            lock (_locker)
            {
                base.Add(editions);
                _dictRepository.TryRemove(typeof(T), out var _);
            }
        }

        public override IEnumerable<T> GetAll<T>()
        {
            MetricsContext.IsCacheHit.Value = false;

            if (_dictRepository.TryGetValue(typeof(T), out var repository))
            {
                MetricsContext.IsCacheHit.Value = true;
                return (IEnumerable<T>)repository;
            }
            else
            {
                lock (_locker)
                {
                    if (_dictRepository.TryGetValue(typeof(T), out var _repository))
                    {
                        MetricsContext.IsCacheHit.Value = true;
                        return (IEnumerable<T>)_repository;
                    }

                    var dataFromFile = base.GetAll<T>();

                    _dictRepository[typeof(T)] = dataFromFile;

                    return dataFromFile;
                }
            }
        }

        public override void Remove<T>(IEnumerable<T> editions)
        {
            lock (_locker)
            {
                base.Remove(editions);

                _dictRepository.TryRemove(typeof(T), out _);
            }
        }
        public override T GetByName<T>(string name) where T : class
        {
            var allItems = this.GetAll<T>().ToList();
            return allItems.FirstOrDefault(e => e.Name == name);
        }
    }
}
