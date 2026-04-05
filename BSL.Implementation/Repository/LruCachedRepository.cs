using BSL.Implementation.Metrics;
using BSL.Implementation.Repository;
using BSL.Models;
using BSL.Models.Interface;
using System.Collections.Generic;
using System.Threading;

namespace BSL.Implementation.Repository
{
    /// <summary>
    /// Репозиторий с кэшированием по стратегии LRU (Least Recently Used).
    /// Используется в качестве базовой модели (baseline) для оценки производительности.
    /// </summary>
    public class LruCachedRepository : RepositoryDecorator
    {
        private readonly int _capacity;
        private readonly Dictionary<string, LinkedListNode<CacheItem>> _cacheMap;
        private readonly LinkedList<CacheItem> _lruList;

        private readonly ReaderWriterLockSlim _lock = new();

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

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="LruCachedRepository"/>.
        /// </summary>
        /// <param name="repository">Базовый репозиторий для декорации.</param>
        /// <param name="capacity">Максимальная вместимость кэша.</param>
        public LruCachedRepository(IRepository repository, int capacity = 1000) : base(repository)
        {
            _capacity = capacity;
            _cacheMap = new Dictionary<string, LinkedListNode<CacheItem>>(capacity);
            _lruList = new LinkedList<CacheItem>();
        }

        private string GenerateKey<T>(string name) => $"{typeof(T).Name}_{name}";

        public override T? GetByName<T>(string name) where T : class
        {
            MetricsContext.IsCacheHit.Value = false;
            var cacheKey = GenerateKey<T>(name);

            _lock.EnterUpgradeableReadLock();
            try
            {
                if (_cacheMap.TryGetValue(cacheKey, out var node))
                {
                    MetricsContext.IsCacheHit.Value = true;

                    _lock.EnterWriteLock();
                    try
                    {
                        if (node.List != null)
                        {
                            _lruList.Remove(node);
                            _lruList.AddFirst(node);
                        }
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }

                    return node.Value.Value as T;
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }

            var itemFromDb = base.GetByName<T>(name);
            if (itemFromDb != null)
            {
                AddOrUpdateCache(cacheKey, itemFromDb as Edition);
            }

            return itemFromDb;
        }

        private void AddOrUpdateCache(string key, Edition? item)
        {
            if (item == null) return;

            _lock.EnterWriteLock();
            try
            {
                if (_cacheMap.TryGetValue(key, out var existingNode))
                {
                    _lruList.Remove(existingNode);
                    _lruList.AddFirst(existingNode);
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
                _lock.ExitWriteLock();
            }
        }

        public override void Remove<T>(IEnumerable<T> editions)
        {
            _lock.EnterWriteLock();
            try
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
            finally
            {
                _lock.ExitWriteLock();
            }
            base.Remove(editions);
        }

        public override void Add<T>(IEnumerable<T> editions)
        {
            Remove(editions);
            base.Add(editions);
        }
    }
}