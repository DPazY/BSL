using BSL.Implementation.Metrics;
using BSL.Models.Interface;
using System.Collections.Concurrent;

namespace BSL.Implementation.Repository
{
    public class CachedRepository : RepositoryDecorator
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<Type, object> _dictRepository = new();

        public CachedRepository(IRepository innerRepository) : base(innerRepository)
        {
        }

        public override async Task Add<T>(IEnumerable<T> editions)
        {
            await _semaphore.WaitAsync();
            try
            {
                await base.Add(editions);
                _dictRepository.TryRemove(typeof(T), out _);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override async Task<IEnumerable<T>> GetAll<T>()
        {
            MetricsContext.Current.Value = new CacheMetricState { IsHit = false };

            if (_dictRepository.TryGetValue(typeof(T), out var repository))
            {
                if (MetricsContext.Current.Value != null)
                {
                    MetricsContext.Current.Value.IsHit = true;
                }

                return (IEnumerable<T>)repository;
            }
            else
            {
                await _semaphore.WaitAsync();
                try
                {
                    if (_dictRepository.TryGetValue(typeof(T), out var _repository))
                    {
                        if (MetricsContext.Current.Value != null)
                        {
                            MetricsContext.Current.Value.IsHit = true;
                        }
                        
                        return (IEnumerable<T>)_repository;
                    }

                    var dataFromFile = await base.GetAll<T>();

                    _dictRepository[typeof(T)] = dataFromFile;

                    return dataFromFile;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        public override async Task Remove<T>(IEnumerable<T> editions)
        {
            await _semaphore.WaitAsync();
            try
            {
                await base.Remove(editions);
                _dictRepository.TryRemove(typeof(T), out _);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override async Task<T> GetByName<T>(string name)
        {
            var allItems = await this.GetAll<T>();
            return allItems.FirstOrDefault(e => e.Name == name);
        }
    }
}