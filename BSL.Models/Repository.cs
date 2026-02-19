using System.IO.Abstractions;
using System.Text.Json;

namespace BSL.Models
{
    public class Repository : IRepository
    {
        private IFileSystem _fileSystem;
        private string _filePath;
        private readonly JsonSerializerOptions _jsonOptions;
        private IEnumerable<Book>? bookRepository = null;
        private IEnumerable<Newspaper>? newspaperRepository = null;
        private IEnumerable<Patent>? patentRepository = null;
        private IEnumerable<Edition>? editionRepository = null;


        public Repository(IFileSystem fileSystem, string filePath, JsonSerializerOptions jsonOptions)
        {
            this._fileSystem = fileSystem;
            this._filePath = filePath;
            this._jsonOptions = jsonOptions;
        }

        public IEnumerable<T> GetAll<T>()
        {
            if (!_fileSystem.File.Exists(_filePath)) return Enumerable.Empty<T>();

            return typeof(T) switch
            {
                Type t when t == typeof(Newspaper) =>
                    (IEnumerable<T>)(newspaperRepository ?? LoadFromFile<Newspaper>()),

                Type t when t == typeof(Book) =>
                    (IEnumerable<T>)(bookRepository ?? LoadFromFile<Book>()),

                Type t when t == typeof(Patent) =>
                    (IEnumerable<T>)(patentRepository ?? LoadFromFile<Patent>()),

                Type t when t == typeof(Edition) =>
                    (IEnumerable<T>)(editionRepository ?? LoadFromFile<Edition>()),

                _ => throw new NotSupportedException($"Тип {typeof(T).Name} не поддерживается")
            };
        }

        private IEnumerable<T> LoadFromFile<T>()
        {
            var command = _fileSystem.File.ReadAllLines(_filePath)
                .Select(line => JsonSerializer.Deserialize<T>(line, _jsonOptions)!)
                .ToList();
            var type = typeof(T);
            switch (type.Name)
            {
                case "Newspaper":
                    newspaperRepository = (IEnumerable<Newspaper>)command;
                        break;
                case "Book":
                    bookRepository = (IEnumerable<Book>)command;
                        break;
                case "Patent":
                    patentRepository = (IEnumerable<Patent>)command;
                        break;
                case "Edition":
                    editionRepository = (IEnumerable<Edition>)command;
                        break;
                default: throw new NotSupportedException($"Тип {typeof(T).Name} не поддерживается");
            }
            return command;
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
            Add(updateElements);
        }
    }
}


