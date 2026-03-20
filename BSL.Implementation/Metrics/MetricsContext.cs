namespace BSL.Implementation.Metrics
{
    public static class MetricsContext
    {
        public static readonly AsyncLocal<bool> IsCacheHit = new AsyncLocal<bool>();
    }
}
