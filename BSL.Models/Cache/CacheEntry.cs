public class CacheEntry
{
    public object Data { get; set; }

    public long HitCount;

    public double FetchDurationMs { get; set; }
    public long SizeBytes { get; set; }

    public double CalculateRho()
    {
        var currentHits = Interlocked.Read(ref HitCount);
        return (currentHits * FetchDurationMs) / Math.Max(1, SizeBytes);
    }
}