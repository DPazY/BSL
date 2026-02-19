using System.IO.Abstractions;
using System.Text.Json;

namespace BSL.Models
{
    public class Repository : IRepository
    {
        private IFileSystem _fileSystem;
        private string _filePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public Repository(IFileSystem fileSystem, string filePath, JsonSerializerOptions jsonOptions)
        {
            this._fileSystem = fileSystem;
            this._filePath = filePath;
            this._jsonOptions = jsonOptions;
        }

        public IEnumerable<T> GetAll<T>()
        {
            if (!_fileSystem.File.Exists(_filePath)) return new List<T>();

            return _fileSystem.File.ReadAllLines(_filePath)
                .Select(line => JsonSerializer.Deserialize<T>(line, _jsonOptions))
                .ToList();
        }

        public void Add<T>(IEnumerable<T> editons)
        {
            var lines = editons.Select(p => JsonSerializer.Serialize(p, _jsonOptions));
            _fileSystem.File.WriteAllLines(_filePath, lines);
        }

        public void Remove<T>(IEnumerable<T> editons)
        {
            var elements = GetAll<T>();
            var updateElements = elements.Except(editons).ToList();
            Add<T>(updateElements);
        }
    }
}


