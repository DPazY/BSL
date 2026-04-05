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
        private readonly object _queueLock = new();

        private readonly ITelemetryAggregator _telemetryAggregator;

        private readonly long _maxMemoryBytes;
        private long _currentMemoryBytes = 0;

        public IfsCachedRepository(
            IRepository innerRepository,
            ITelemetryAggregator telemetryAggregator,
            long maxMemoryBytes)
            : base(innerRepository)
        {
            _telemetryAggregator = telemetryAggregator;
            _maxMemoryBytes = maxMemoryBytes;
        }

        public override T? GetByName<T>(string name) where T : class
        {
            string cacheKey = $"{typeof(T).Name}:{name}";

            // 1. Асинхронная запись в телеметрию (должна использовать Channels или ConcurrentQueue внутри)
            _telemetryAggregator.RecordHit(cacheKey);

            // 2. Lock-free чтение из кэша
            if (_cache.TryGetValue(cacheKey, out var entry))
            {
                // 3. Атомарное увеличение счетчиков на уровне процессора (никаких lock!)
                Interlocked.Increment(ref entry.HitCount);

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
                    HitCount = 1,
                    FetchDurationMs = sw.Elapsed.TotalMilliseconds,
                    SizeBytes = ObjectSizeApproximator.EstimateSizeBytes(dataFromFile)
                };

                EnsureMemoryCapacity(newEntry.SizeBytes);

                _cache[cacheKey] = newEntry;
                Interlocked.Add(ref _currentMemoryBytes, newEntry.SizeBytes);
            }

            return dataFromFile;
        }

        /// <summary>
        /// Ленивое вытеснение (Lazy Eviction) на основе удельной полезности.
        /// Вызывается только при превышении лимита памяти (Cache Miss).
        /// </summary>
        /// <param name="requiredBytes">Требуемый объем памяти для нового элемента.</param>
        private void EnsureMemoryCapacity(long requiredBytes)
        {
            if (Interlocked.Read(ref _currentMemoryBytes) + requiredBytes <= _maxMemoryBytes)
                return;

            lock (_queueLock)
            {
                if (Interlocked.Read(ref _currentMemoryBytes) + requiredBytes <= _maxMemoryBytes)
                    return;

                var evictionCandidates = _cache
                    .Select(kvp => new
                    {
                        Key = kvp.Key,
                        Rho = kvp.Value.CalculateRho(),
                        Size = kvp.Value.SizeBytes
                    })
                    .OrderBy(x => x.Rho)
                    .ToList();

                foreach (var candidate in evictionCandidates)
                {
                    if (_cache.TryRemove(candidate.Key, out var evictedEntry))
                    {
                        Interlocked.Add(ref _currentMemoryBytes, -evictedEntry.SizeBytes);

                        if (Interlocked.Read(ref _currentMemoryBytes) + requiredBytes <= _maxMemoryBytes)
                        {
                            break;
                        }
                    }
                }
            }
        }
    }
}
