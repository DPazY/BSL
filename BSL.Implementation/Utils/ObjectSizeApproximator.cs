using BSL.Models;
using System.Text.Json;

namespace BSL.Implementation
{
    public class ObjectSizeApproximator
    {
        public static long EstimateSizeBytes<T>(T dataFromFile) where T : Edition => 1024;
    }
}
