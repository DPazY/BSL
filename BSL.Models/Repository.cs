using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Text.Json;

namespace BSL.Models
{
    public class Repository : IRepository
    {
        private readonly object _locker;
        private readonly IFileSystem _fileSystem;
        private readonly string _directoryPath;
        private readonly ConcurrentDictionary<Type, string> _dictFilePath = new ConcurrentDictionary<Type, string>();
        private readonly JsonSerializerOptions? _jsonOptions;
        private readonly ConcurrentDictionary<Type, object> _dictRepository = new ConcurrentDictionary<Type, object>();
        

        public Repository(IFileSystem fileSystem, string directoryPath, JsonSerializerOptions? jsonOptions = null)
        {
            _fileSystem = fileSystem;
            _directoryPath = directoryPath;
            _jsonOptions = jsonOptions;
            _locker = new object();
        }
        private IEnumerable<T> LoadFromFile<T>()
        {
            lock (_locker)
            {
                return (IEnumerable<T>)_dictRepository.GetOrAdd(typeof(T), t =>
                _fileSystem.File.ReadAllLines(GetFilePath<T>())
                    .Select(line => JsonSerializer.Deserialize<T>(line, _jsonOptions)!)
                    .ToList());
            }
        }

        private string GetFilePath<T>()
        {
            return _dictFilePath.GetOrAdd(typeof(T), t =>
            _fileSystem.Path.Combine(_directoryPath, $"{typeof(T).Name}s"));
        }

        public IEnumerable<T> GetAll<T>()
        {
            if (!_fileSystem.File.Exists(GetFilePath<T>()))
            {
                return Enumerable.Empty<T>();
            }
            return _dictRepository.TryGetValue(typeof(T), out var repository) ? (IEnumerable<T>)repository : LoadFromFile<T>();
        }


        public void Add<T>(IEnumerable<T> editions)
        {
            var lines = editions.Select(p => JsonSerializer.Serialize(p, _jsonOptions)).ToList();
            lock (_locker)
            {
                _fileSystem.File.WriteAllLines(GetFilePath<T>(), lines);
                _dictRepository.TryRemove(typeof(T), out var obj);
            }
        }

        public void Remove<T>(IEnumerable<T> editions)
        {
            var elements = GetAll<T>();
            var updateElements = elements.Except(editions).ToList();
            Add(updateElements);
        }
    }
}


