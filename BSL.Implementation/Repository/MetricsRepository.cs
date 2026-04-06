using BSL.Implementation.Metrics;
using BSL.Models.Interface;
using System.Diagnostics;

namespace BSL.Implementation.Repository
{
    public class MetricsRepository : RepositoryDecorator
    {
        private AppMetrics _appMetrics;
        public MetricsRepository(IRepository repository, AppMetrics appMetrics) : base(repository)
        {
            _appMetrics = appMetrics;
        }

        public override async Task<IEnumerable<T>> GetAll<T>()
        {
            MetricsContext.Current.Value = new CacheMetricState { IsHit = false };

            var stopWatch = Stopwatch.StartNew();

            var result = await base.GetAll<T>();
            stopWatch.Stop();

            _appMetrics.RecordMethodExecution(
              methodName: "GetAll",
              targetType: typeof(T).Name,
              durationMs: stopWatch.Elapsed.TotalMilliseconds);

            return result;
        }

        public override async Task Add<T>(IEnumerable<T> editions)
        {
            var stopWatch = Stopwatch.StartNew();

            await base.Add(editions);
            stopWatch.Stop();

            _appMetrics.RecordMethodExecution(
               methodName: "Add",
               targetType: typeof(T).Name,
               durationMs: stopWatch.Elapsed.TotalMilliseconds);
        }

        public override async Task Remove<T>(IEnumerable<T> editions)
        {
            var stopWatch = Stopwatch.StartNew();

            await base.Remove(editions);
            stopWatch.Stop();

            _appMetrics.RecordMethodExecution(
               methodName: "Remove",
               targetType: typeof(T).Name,
               durationMs: stopWatch.Elapsed.TotalMilliseconds);
        }

        public override async Task<T> GetByName<T>(string name) where T : class
        {
            MetricsContext.Current.Value = new CacheMetricState { IsHit = false };
            
            var stopWatch = Stopwatch.StartNew();

            var result = await base.GetByName<T>(name);
            stopWatch.Stop();

            _appMetrics.RecordMethodExecution(
                methodName: "GetByName",
                targetType: typeof(T).Name,
                durationMs: stopWatch.Elapsed.TotalMilliseconds,
                isCacheHit: MetricsContext.Current.Value.IsHit);


            return result;
        }
    }
}
