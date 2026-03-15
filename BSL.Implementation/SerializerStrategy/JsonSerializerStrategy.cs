using BSL.Models.Interface;
using System.Text.Json;


namespace BSL.Implementation.SerializerStrategy
{
    public class JsonSerializerStrategy : ISerializerStrategy
    {
        private readonly JsonSerializerOptions? _jsonOptions;
        public JsonSerializerStrategy(JsonSerializerOptions? jsonSerializerOptions = null)
        {
            _jsonOptions = jsonSerializerOptions;
        }
        public IEnumerable<T> Deserialize<T>(Stream stream)
        {
            using var reader = new StreamReader(stream);
            var list = new List<T>();

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var item = JsonSerializer.Deserialize<T>(line, _jsonOptions);
                if (item != null) list.Add(item);
            }

            return list;
        }

        public void Serialize<T>(IEnumerable<T> values, Stream? stream = null)
        {
            ArgumentNullException.ThrowIfNull(stream, nameof(stream));
            using var writer = new StreamWriter(stream);
            foreach (var item in values)
            {
                writer.WriteLine(JsonSerializer.Serialize<T>(item, _jsonOptions));
            }
        }
    }
}

