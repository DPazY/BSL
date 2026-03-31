using BSL.Implementation.Metrics;
using BSL.Models;
using BSL.Models.Interface;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace BSL.Implementation.Repository
{
    public class IfsCachedRepository : RepositoryDecorator
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
        private readonly PriorityQueue<(string Key, int Version), double> _evictionQueue = new();
        private readonly object _queueLock = new();

        private readonly ITelemetryAggregator _telemetryAggregator;

        private readonly AppMetrics _metrics;
        private readonly long _maxMemoryBytes;
        private long _currentMemoryBytes = 0;

        public IfsCachedRepository(
            IRepository innerRepository,
            ITelemetryAggregator telemetryAggregator,
            AppMetrics metrics,
            long maxMemoryBytes)
            : base(innerRepository)
        {
            _telemetryAggregator = telemetryAggregator;
            _metrics = metrics;
            _maxMemoryBytes = maxMemoryBytes;
        }

        public override T? GetByName<T>(string name) where T : class
        {
            string cacheKey = $"{typeof(T).Name}:{name}";

            _telemetryAggregator.RecordHit(cacheKey);

            if (_cache.TryGetValue(cacheKey, out var entry))
            {
                lock (entry)
                {
                    entry.RequestRateLambda += 1.0;
                    entry.Version++;

                    lock (_queueLock)
                    {
                        _evictionQueue.Enqueue((cacheKey, entry.Version), entry.CalculateRho());
                    }
                }

                MetricsContext.IsCacheHit.Value = true;
                return entry.Data as T;
            }

            MetricsContext.IsCacheHit.Value = false;
            var sw = Stopwatch.StartNew();

            var dataFromFile = base.GetByName<T>(name);
            sw.Stop();

            if (dataFromFile != null)
            {
                var newEntry = new CacheEntry
                {
                    Data = dataFromFile,
                    RequestRateLambda = 1.0,
                    FetchDurationMs = sw.Elapsed.TotalMilliseconds,
                    // Предполагается, что метод EstimateSizeBytes у тебя реализован
                    SizeBytes = ObjectSizeApproximator.EstimateSizeBytes(dataFromFile),
                    Version = 1
                };

                EnsureMemoryCapacity(newEntry.SizeBytes);

                _cache[cacheKey] = newEntry;
                Interlocked.Add(ref _currentMemoryBytes, newEntry.SizeBytes);

                lock (_queueLock)
                {
                    _evictionQueue.Enqueue((cacheKey, newEntry.Version), newEntry.CalculateRho());
                }
            }

            return dataFromFile;
        }

        /// <summary>
        /// Реактивное вытеснение (KKT-Eviction) с фильтрацией ghost-элементов.
        /// </summary>
        private void EnsureMemoryCapacity(long requiredBytes)
        {
            if (Interlocked.Read(ref _currentMemoryBytes) + requiredBytes <= _maxMemoryBytes)
                return;

            lock (_queueLock)
            {
                while (Interlocked.Read(ref _currentMemoryBytes) + requiredBytes > _maxMemoryBytes && _evictionQueue.Count > 0)
                {
                    var (key, queuedVersion) = _evictionQueue.Dequeue();

                    if (!_cache.TryGetValue(key, out var currentEntry) || currentEntry.Version != queuedVersion)
                    {
                        continue;
                    }

                    if (_cache.TryRemove(key, out var evictedEntry))
                    {
                        Interlocked.Add(ref _currentMemoryBytes, -evictedEntry.SizeBytes);
                    }
                }
            }
        }
    }
}
