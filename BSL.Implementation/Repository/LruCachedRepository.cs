using BSL.Implementation.Metrics;
using BSL.Models;
using BSL.Models.Interface;

namespace BSL.Implementation.Repository
{
    public class LruCachedRepository : RepositoryDecorator
    {
        private readonly int _capacity;
        private readonly Dictionary<string, LinkedListNode<CacheItem>> _cacheMap;
        private readonly LinkedList<CacheItem> _lruList;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private class CacheItem
        {
            public string Key { get; }
            public Edition Value { get; }
            public CacheItem(string key, Edition value)
            {
                Key = key;
                Value = value;
            }
        }

        public LruCachedRepository(IRepository repository, int capacity = 10) : base(repository)
        {
            _capacity = capacity;
            _cacheMap = new Dictionary<string, LinkedListNode<CacheItem>>(capacity);
            _lruList = new LinkedList<CacheItem>();
        }

        private string GenerateKey<T>(string name) => $"{typeof(T).Name}_{name}";

        public override async Task<T> GetByName<T>(string name) where T : class
        {
            var cacheKey = GenerateKey<T>(name);

            await _semaphore.WaitAsync();
            try
            {
                if (_cacheMap.TryGetValue(cacheKey, out var node))
                {
                    if (MetricsContext.Current.Value != null)
                    {
                        MetricsContext.Current.Value.IsHit = true;
                    }

                    if (node.List != null)
                    {
                        _lruList.Remove(node);
                        _lruList.AddFirst(node);
                    }

                    return node.Value.Value as T;
                }
            }
            finally
            {
                _semaphore.Release();
            }

            var itemFromDb = await base.GetByName<T>(name);
            if (itemFromDb != null)
            {
                await AddOrUpdateCacheAsync(cacheKey, itemFromDb as Edition);
            }

            return itemFromDb;
        }

        private async Task AddOrUpdateCacheAsync(string key, Edition? item)
        {
            if (item == null) return;

            await _semaphore.WaitAsync();
            try
            {
                if (_cacheMap.TryGetValue(key, out var existingNode))
                {
                    if (existingNode.List != null)
                    {
                        _lruList.Remove(existingNode);
                        _lruList.AddFirst(existingNode);
                    }
                    return;
                }

                if (_cacheMap.Count >= _capacity)
                {
                    var lastNode = _lruList.Last;
                    if (lastNode != null)
                    {
                        _cacheMap.Remove(lastNode.Value.Key);
                        _lruList.RemoveLast();
                    }
                }

                var newNode = new LinkedListNode<CacheItem>(new CacheItem(key, item));
                _lruList.AddFirst(newNode);
                _cacheMap[key] = newNode;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override async Task Remove<T>(IEnumerable<T> editions)
        {
            await _semaphore.WaitAsync();
            try
            {
                foreach (var edition in editions)
                {
                    var cacheKey = GenerateKey<T>(edition.Name);
                    if (_cacheMap.TryGetValue(cacheKey, out var node))
                    {
                        if (node.List != null)
                        {
                            _lruList.Remove(node);
                        }
                        _cacheMap.Remove(cacheKey);
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
            await base.Remove(editions);
        }

        public override async Task Add<T>(IEnumerable<T> editions)
        {
            await Remove(editions);
            await base.Add(editions);
        }
    }
}