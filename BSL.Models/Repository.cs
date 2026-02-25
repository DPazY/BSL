using System.Collections.Concurrent;
using System.IO.Abstractions;

namespace BSL.Models
{
    public class Repository : IRepository
    {
        private readonly object _locker;
        private readonly IFileSystem _fileSystem;
        private readonly string _directoryPath;
        private readonly ConcurrentDictionary<Type, string> _dictFilePath = new ConcurrentDictionary<Type, string>();
        private readonly ConcurrentDictionary<Type, object> _dictRepository = new ConcurrentDictionary<Type, object>();
        private readonly ISerializerStrategy? _serializerStrategy;

        public Repository(IFileSystem fileSystem,
            string directoryPath, ISerializerStrategy? serializerStrategy = null)
        {
            _fileSystem = fileSystem;
            _directoryPath = directoryPath;
            _serializerStrategy = serializerStrategy;
            _locker = new object();
        }
        private IEnumerable<T> LoadFromFile<T>()
        {
            ArgumentNullException.ThrowIfNull(_serializerStrategy);

            lock (_locker)
            {
                using var stream = _fileSystem.File.OpenRead(GetFilePath<T>());
                
                return _serializerStrategy.Deserialize<T>(stream);
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
            ArgumentNullException.ThrowIfNull(_serializerStrategy);

            lock (_locker)
            {
                _fileSystem.File.Delete(GetFilePath<T>());
                using var fileCreated = _fileSystem.File.Create(GetFilePath<T>());
                _serializerStrategy.Serialize(editions, fileCreated);
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


