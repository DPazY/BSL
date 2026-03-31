using BSL.Models;
using System.Text.Json;

namespace BSL.Implementation
{
    public class ObjectSizeApproximator
    {
        public static long EstimateSizeBytes<T>(T dataFromFile) where T : Edition
        {
            using (var stream = new MemoryStream())
            {
                JsonSerializer.Serialize(stream, dataFromFile);
                return stream.Length;
            }
        }
    }
}
