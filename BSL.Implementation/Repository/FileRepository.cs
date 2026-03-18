using BSL.Models.Interface;
using BSL.Models;
using System.Collections.Concurrent;
using System.IO.Abstractions;

namespace BSL.Implementation.Repository
{
    public class FileRepository : IRepository
    {
        private readonly IFileSystem _fileSystem;
        private readonly AppSettings _appSetings;
        private readonly ConcurrentDictionary<Type, string> _dictFilePath = new ConcurrentDictionary<Type, string>();
        private readonly ISerializerStrategy _serializerStrategy;

        public FileRepository(IFileSystem fileSystem,
            AppSettings appSetings,
            ISerializerStrategy serializerStrategy)
        {
            _fileSystem = fileSystem;
            _appSetings = appSetings;
            _serializerStrategy = serializerStrategy;
        }
        private IEnumerable<T> LoadFromFile<T>()
        {
            ArgumentNullException.ThrowIfNull(_serializerStrategy);

            using var stream = _fileSystem.File.OpenRead(GetFilePath<T>());
            return _serializerStrategy.Deserialize<T>(stream);
        }

        private string GetFilePath<T>()
        {
            return _dictFilePath.GetOrAdd(typeof(T), t =>
            _fileSystem.Path.Combine(_appSetings.WorkDirectory, $"{typeof(T).Name}s" + _appSetings.FileExtension));
        }

        public IEnumerable<T> GetAll<T>() where T : Edition
        {
            if (!_fileSystem.File.Exists(GetFilePath<T>()))
            {
                return Enumerable.Empty<T>();
            }
            return LoadFromFile<T>();
        }

        public void Add<T>(IEnumerable<T> editions) where T : Edition
        {
            ArgumentNullException.ThrowIfNull(_serializerStrategy);

            _fileSystem.File.Delete(GetFilePath<T>());
            using var fileCreated = _fileSystem.File.Create(GetFilePath<T>());

            _serializerStrategy.Serialize(editions, fileCreated);

        }

        public void Remove<T>(IEnumerable<T> editions) where T : Edition
        {
            var elements = GetAll<T>();
            var updateElements = elements.Except(editions).ToList();
            Add(updateElements);
        }
    }
}


