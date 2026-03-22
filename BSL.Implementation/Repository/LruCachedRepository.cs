using BSL.Implementation.Metrics;
using BSL.Models;
using BSL.Models.Interface;
using System.Collections.Generic;

namespace BSL.Implementation.Repository
{
    public class LruCachedRepository : RepositoryDecorator
    {
        private readonly int _capacity;
        private readonly Dictionary<string, LinkedListNode<CacheItem>> _cacheMap;
        private readonly LinkedList<CacheItem> _lruList;
        private readonly object _lock = new();

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

        public LruCachedRepository(IRepository repository, int capacity = 1000) : base(repository)
        {
            _capacity = capacity;
            _cacheMap = new Dictionary<string, LinkedListNode<CacheItem>>(capacity);
            _lruList = new LinkedList<CacheItem>();
        }

        private string GenerateKey<T>(string name) => $"{typeof(T).Name}_{name}";

        public override T GetByName<T>(string name)
        {
            MetricsContext.IsCacheHit.Value = false;
            var cacheKey = GenerateKey<T>(name);

            lock (_lock)
            {
                if (_cacheMap.TryGetValue(cacheKey, out var node))
                {
                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                    MetricsContext.IsCacheHit.Value = true;
                    return node.Value.Value as T;
                }
            }

            var itemFromDb = base.GetByName<T>(name);

            if (itemFromDb != null)
            {
                lock (_lock)
                {
                    if (_cacheMap.ContainsKey(cacheKey))
                    {
                        return itemFromDb;
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

                    var newNode = new LinkedListNode<CacheItem>(new CacheItem(cacheKey, itemFromDb));
                    _lruList.AddFirst(newNode);

                    _cacheMap[cacheKey] = newNode;
                }
            }

            return itemFromDb;
        }

        public override void Remove<T>(IEnumerable<T> editions)
        {
            lock (_lock)
            {
                foreach (var edition in editions)
                {
                    var cacheKey = GenerateKey<T>(edition.Name);
                    if (_cacheMap.TryGetValue(cacheKey, out var node))
                    {
                        _lruList.Remove(node);
                        _cacheMap.Remove(cacheKey);
                    }
                }
            }
            base.Remove(editions);
        }

        public override void Add<T>(IEnumerable<T> editions)
        {
            lock (_lock)
            {
                foreach (var edition in editions)
                {
                    var cacheKey = GenerateKey<T>(edition.Name);
                    if (_cacheMap.TryGetValue(cacheKey, out var node))
                    {
                        _lruList.Remove(node);
                        _cacheMap.Remove(cacheKey);
                    }
                }
            }
            base.Add(editions);
        }
    }
}