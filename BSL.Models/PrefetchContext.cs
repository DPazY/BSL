namespace BSL.Models
{
    public static class PrefetchContext
    {
        private static readonly AsyncLocal<bool> _isPrefetching = new();

        public static bool IsPrefetching
        {
            get => _isPrefetching.Value;
            set => _isPrefetching.Value = value;
        }
    }
}