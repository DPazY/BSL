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

        public override IEnumerable<T> GetAll<T>()
        {
            var stopWatch = Stopwatch.StartNew();

            var result = base.GetAll<T>();
            stopWatch.Stop();

            _appMetrics.RecordMethodExecution(
              methodName: "GetAll",
              targetType: typeof(T).Name,
              durationMs: stopWatch.Elapsed.TotalMilliseconds);

            return result;
        }

        public override void Add<T>(IEnumerable<T> editions)
        {
            var stopWatch = Stopwatch.StartNew();

            base.Add(editions);
            stopWatch.Stop();

            _appMetrics.RecordMethodExecution(
               methodName: "Add",
               targetType: typeof(T).Name,
               durationMs: stopWatch.Elapsed.TotalMilliseconds);
        }

        public override void Remove<T>(IEnumerable<T> editions)
        {
            var stopWatch = Stopwatch.StartNew();

            base.Remove(editions);
            stopWatch.Stop();

            _appMetrics.RecordMethodExecution(
               methodName: "Remove",
               targetType: typeof(T).Name,
               durationMs: stopWatch.Elapsed.TotalMilliseconds);
        }

        public override T? GetByName<T>(string name) where T : class
        {
            var stopWatch = Stopwatch.StartNew();

            var result = base.GetByName<T>(name);
            stopWatch.Stop();

            _appMetrics.RecordMethodExecution(
                methodName: "GetByName",
                targetType: typeof(T).Name,
                durationMs: stopWatch.Elapsed.TotalMilliseconds);

            return result;
        }
    }
}
