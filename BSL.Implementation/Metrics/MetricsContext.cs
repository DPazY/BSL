namespace BSL.Implementation.Metrics
{
    public static class MetricsContext
    {
        public static readonly AsyncLocal<CacheMetricState> Current = new AsyncLocal<CacheMetricState>();
    }

    public class CacheMetricState
    {
        public bool IsHit { get; set; }
    }
}
