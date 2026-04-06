using BSL.Implementation.Repository;
using BSL.Implementation.SerializerStrategy;
using BSL.Models;
using FluentAssertions;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace BSL.Test.Repository
{
    public partial class RepositoryTest
    {
        private MockFileSystem _mockFileSystem;
        private string _testPath = @"C:\data";
        private JsonSerializerOptions _jsonOptions;
        private AppSettings _appSettings;

        List<Edition> editions = new List<Edition>()
{
    new Newspaper(
        name: "Комсомольская правда",
        placeOfPublication: "Москва",
        publishingHouse: "ИД «Комсомольская правда»",
        numberOfPages: 16,
        notes: "Ежедневная общественно-политическая газета",
        issueNumber: 15430,
        dataPublishing: new DateOnly(2023, 10, 5),
        issn: "0233-4399"
    ),

    new Newspaper(
        name: "The New York Times",
        placeOfPublication: "Нью-Йорк, США",
        publishingHouse: "The New York Times Company",
        numberOfPages: 64,
        notes: null,
        issueNumber: 58201,
        dataPublishing: new DateOnly(2003, 10, 5),
        issn: "0362-4331"
    ),

    new Newspaper(
        name: "Ведомости",
        placeOfPublication: "Москва",
        publishingHouse: "АО «Бизнес Ньюс Медиа»",
        numberOfPages: 32,
        notes: "Деловая газета",
        issueNumber: 450,
        dataPublishing: new DateOnly(1999, 9, 21),
        issn: "1562-2584"
    ),
    new Book("Преступление и наказание", new DateOnly(2000, 1, 1), "БББ", "Толстой"),
    new Book("Идиот", new DateOnly(2003, 1, 1), "аба", "Толстой Мартин"),
    new Book("Песнь Льда и Пламени", new DateOnly(1978, 1, 1), "ААА", "Мартин"),
    new Patent(
            name: "Способ беспроводной передачи данных",
        inventor: "Иванов И.И., Петров П.П.",
        country: "Россия",
        registrationNumber: "RU2745000",
        submissionDate: new DateOnly(2020, 5, 15),
        publicationDate: new DateOnly(2021, 11, 20),
        numberOfPages: 24,
        notes: "Приоритетный патент"
        ),

        new Patent(
            name: "Устройство для очистки воды",
            inventor: "Smith John",
            country: "USA",
            registrationNumber: "US1029384",
            submissionDate: new DateOnly(1990, 1, 10),
            publicationDate: new DateOnly(1992, 3, 5),
            numberOfPages: 12,
            notes: null
        )
        };

        [SetUp]
        public void Setup()
        {
            _mockFileSystem = new MockFileSystem();
            _mockFileSystem.AddDirectory(@"C:\data");
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                WriteIndented = false
            };
            _appSettings = new AppSettings(_testPath, _testPath, Models.Enum.ProcessedFileAction.None, 0, ".json");
        }

        [Test]
        public async Task GetAll_WhenFileExists_Json_ReturnCorrectCount()
        {
            var line1 = "{\"name\":\"Комсомольская правда\",\"placeOfPublication\":\"Москва\",\"publishingHouse\":\"ИД «Комсомольская правда»\",\"numberOfPages\":16,\"notes\":\"Ежедневная общественно-политическая газета\",\"issueNumber\":15430,\"dataPublishing\":\"2023-10-05\",\"issn\":\"0233-4399\"}";
            var line2 = "{\"name\":\"The New York Times\",\"placeOfPublication\":\"Нью-Йорк, США\",\"publishingHouse\":\"The New York Times Company\",\"numberOfPages\":64,\"notes\":null,\"issueNumber\":58201,\"dataPublishing\":\"2003-10-05\",\"issn\":\"0362-4331\"}";
            var line3 = "{\"name\":\"Ведомости\",\"placeOfPublication\":\"Москва\",\"publishingHouse\":\"АО «Бизнес Ньюс Медиа»\",\"numberOfPages\":32,\"notes\":\"Деловая газета\",\"issueNumber\":450,\"dataPublishing\":\"1999-09-21\",\"issn\":\"1562-2584\"}";
            _mockFileSystem.AddFile(_mockFileSystem.Path.Combine(_testPath, $"Newspapers" + _appSettings.FileExtension), new MockFileData($"{line1}\n{line2}\n{line3}"));

            var fileRepo = new FileRepository(_mockFileSystem, _appSettings, new JsonSerializerStrategy(_jsonOptions));
            var repo = new CachedRepository(fileRepo);

            var result = await repo.GetAll<Newspaper>();

            Assert.That(result.Count, Is.EqualTo(3));
            result.Should().BeEquivalentTo(new[] { editions[0], editions[1], editions[2] });

        }

        [Test]
        public async Task Add_Json_ShouldSaveToFileInOneLine()
        {
            var fileRepo = new FileRepository(_mockFileSystem, _appSettings, new JsonSerializerStrategy(_jsonOptions));
            var repo = new CachedRepository(fileRepo);

            var newItem = new List<Newspaper>(){ new Newspaper(
                name: "Комсомольская правда",
                placeOfPublication: "Москва",
                publishingHouse: "ИД «Комсомольская правда»",
                numberOfPages: 16,
                notes: "Ежедневная общественно-политическая газета",
                issueNumber: 15430,
                dataPublishing: new DateOnly(2023, 10, 5),
                issn: "0233-4399"
            ) };
            await repo.Add(newItem);

            var fileLines = _mockFileSystem.File.ReadAllLines(_mockFileSystem.Path.Combine(_testPath, $"Newspapers" + _appSettings.FileExtension));
            Assert.That(fileLines.Length, Is.EqualTo(1));
            Assert.That(fileLines[0], Does.Contain("ИД «Комсомольская правда»"));
            Assert.That(fileLines[0], Does.Not.Contain(Environment.NewLine));
        }

        [Test]
        public async Task Remove_ShouldDeleteSpecificItemsFromFile_Json()
        {
            var item1 = new Newspaper("Газета 1", "Москва", "Дом 1", 10, null, 1, new DateOnly(2000, 1, 1), "111");
            var item2 = new Newspaper("Газета 2", "Питер", "Дом 2", 20, null, 2, new DateOnly(2010, 1, 1), "222");
            var item3 = new Newspaper("Газета 3", "Омск", "Дом 3", 30, null, 3, new DateOnly(2020, 1, 1), "333");

            var line1 = JsonSerializer.Serialize(item1, _jsonOptions);
            var line2 = JsonSerializer.Serialize(item2, _jsonOptions);
            var line3 = JsonSerializer.Serialize(item3, _jsonOptions);
            _mockFileSystem.AddFile(_mockFileSystem.Path.Combine(_testPath, $"Newspapers" + _appSettings.FileExtension), new MockFileData($"{line1}\n{line2}\n{line3}"));

            var fileRepo = new FileRepository(_mockFileSystem, _appSettings, new JsonSerializerStrategy(_jsonOptions));
            var repo = new CachedRepository(fileRepo);

            var itemsToRemove = new List<Newspaper> { item2 };

            await repo.Remove(itemsToRemove);

            var fileLines = _mockFileSystem.File.ReadAllLines(_mockFileSystem.Path.Combine(_testPath, $"Newspapers" + _appSettings.FileExtension));

            Assert.Multiple(() =>
            {
                Assert.That(fileLines.Length, Is.EqualTo(2), "Количество строк в файле должно уменьшиться");

                Assert.That(fileLines.Any(l => l.Contains("Газета 2")), Is.False, "Газета 2 должна быть удалена");

                Assert.That(fileLines.Any(l => l.Contains("Газета 1")), Is.True);
                Assert.That(fileLines.Any(l => l.Contains("Газета 3")), Is.True);
            });
        }

        [Test]
        public async Task GetAll_WhenFileExists_Protobuf_ReturnCorrectCount()
        {
            var strategy = new ProtobufSerializerStrategy();
            using var ms = new MemoryStream();

            var items = editions.Take(3).Cast<Newspaper>().ToList();
            strategy.Serialize(items, ms);

            _mockFileSystem.AddFile(_mockFileSystem.Path.Combine(_testPath, "Newspapers" + _appSettings.FileExtension), new MockFileData(ms.ToArray()));

            var fileRepo = new FileRepository(_mockFileSystem, _appSettings, strategy);
            var repo = new CachedRepository(fileRepo);

            var result = (await repo.GetAll<Newspaper>()).ToList();

            Assert.That(result.Count, Is.EqualTo(3));
            result.Should().BeEquivalentTo(new[] { editions[0], editions[1], editions[2] });
        }

        [Test]
        public async Task Add_Protobuf_ShouldSaveToBinaryFile()
        {
            var strategy = new ProtobufSerializerStrategy();
            var fileRepo = new FileRepository(_mockFileSystem, _appSettings, strategy);
            var repo = new CachedRepository(fileRepo);

            var newItem = new List<Newspaper> { (Newspaper)editions[0] };
            var filePath = _mockFileSystem.Path.Combine(_testPath, "Newspapers" + _appSettings.FileExtension);

            await repo.Add(newItem);

            Assert.That(_mockFileSystem.FileExists(filePath), Is.True);

            var fileBytes = _mockFileSystem.File.ReadAllBytes(filePath);
            Assert.That(fileBytes.Length, Is.GreaterThan(0));

            using var ms = new MemoryStream(fileBytes);
            var savedItems = strategy.Deserialize<Newspaper>(ms).ToList();

            Assert.That(savedItems.Count, Is.EqualTo(1));
            savedItems[0].Should().BeEquivalentTo(newItem[0]);
        }

        [Test]
        public async Task Remove_ShouldDeleteSpecificItemsFromFile_Protobuf()
        {
            var item1 = new Newspaper("Газета 1", "Москва", "Дом 1", 10, null, 1, new DateOnly(2000, 1, 1), "111");
            var item2 = new Newspaper("Газета 2", "Питер", "Дом 2", 20, null, 2, new DateOnly(2010, 1, 1), "222");
            var item3 = new Newspaper("Газета 3", "Омск", "Дом 3", 30, null, 3, new DateOnly(2020, 1, 1), "333");

            var strategy = new ProtobufSerializerStrategy();
            using var ms = new MemoryStream();

            var initialItems = new List<Newspaper> { item1, item2, item3 };
            strategy.Serialize(initialItems, ms);

            var filePath = _mockFileSystem.Path.Combine(_testPath, "Newspapers" + _appSettings.FileExtension);
            _mockFileSystem.AddFile(filePath, new MockFileData(ms.ToArray()));

            var fileRepo = new FileRepository(_mockFileSystem, _appSettings, strategy);
            var repo = new CachedRepository(fileRepo);

            var itemsToRemove = new List<Newspaper> { item2 };

            await repo.Remove(itemsToRemove);

            var fileBytes = _mockFileSystem.File.ReadAllBytes(filePath);
            using var resultStream = new MemoryStream(fileBytes);

            var remainingItems = strategy.Deserialize<Newspaper>(resultStream).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(remainingItems.Count, Is.EqualTo(2), "Количество элементов должно уменьшиться");

                Assert.That(remainingItems.Any(i => i.Name == "Газета 2"), Is.False, "Газета 2 должна быть удалена");
                Assert.That(remainingItems.Any(i => i.Name == "Газета 1"), Is.True);
                Assert.That(remainingItems.Any(i => i.Name == "Газета 3"), Is.True);
            });
        }

        [Test]
        public async Task GetAll_WhenFileExists_Xml_ReturnCorrectCount()
        {
            var strategy = new XmlSerializerStrategy();
            using var ms = new MemoryStream();

            var items = editions.Take(3).Cast<Newspaper>().ToList();

            strategy.Serialize(items, ms);

            _mockFileSystem.AddFile(_mockFileSystem.Path.Combine(_testPath, "Newspapers" + _appSettings.FileExtension), new MockFileData(ms.ToArray()));

            var fileRepo = new FileRepository(_mockFileSystem, _appSettings, strategy);
            var repo = new CachedRepository(fileRepo);

            var result = (await repo.GetAll<Newspaper>()).ToList();

            Assert.That(result.Count, Is.EqualTo(3));
            result.Should().BeEquivalentTo(new[] { editions[0], editions[1], editions[2] });
        }

        [Test]
        public async Task Add_Xml_ShouldSaveToFile()
        {
            var strategy = new XmlSerializerStrategy();
            var fileRepo = new FileRepository(_mockFileSystem, _appSettings, strategy);
            var repo = new CachedRepository(fileRepo);

            var newItem = new List<Newspaper> { (Newspaper)editions[0] };
            var filePath = _mockFileSystem.Path.Combine(_testPath, "Newspapers" + _appSettings.FileExtension);

            await repo.Add(newItem);

            Assert.That(_mockFileSystem.FileExists(filePath), Is.True);

            var fileBytes = _mockFileSystem.File.ReadAllBytes(filePath);
            Assert.That(fileBytes.Length, Is.GreaterThan(0));

            using var ms = new MemoryStream(fileBytes);
            var savedItems = strategy.Deserialize<Newspaper>(ms).ToList();

            Assert.That(savedItems.Count, Is.EqualTo(1));
            savedItems[0].Should().BeEquivalentTo(newItem[0]);
        }

        [Test]
        public async Task Remove_ShouldDeleteSpecificItemsFromFile_Xml()
        {
            var item1 = new Newspaper("Газета 1", "Москва", "Дом 1", 10, null, 1, new DateOnly(2000, 1, 1), "111");
            var item2 = new Newspaper("Газета 2", "Питер", "Дом 2", 20, null, 2, new DateOnly(2010, 1, 1), "222");
            var item3 = new Newspaper("Газета 3", "Омск", "Дом 3", 30, null, 3, new DateOnly(2020, 1, 1), "333");

            var strategy = new XmlSerializerStrategy();
            using var ms = new MemoryStream();

            var initialItems = new List<Newspaper> { item1, item2, item3 };
            strategy.Serialize(initialItems, ms);

            var filePath = _mockFileSystem.Path.Combine(_testPath, "Newspapers" + _appSettings.FileExtension);
            _mockFileSystem.AddFile(filePath, new MockFileData(ms.ToArray()));

            var fileRepo = new FileRepository(_mockFileSystem, _appSettings, strategy);
            var repo = new CachedRepository(fileRepo);
            

            var itemsToRemove = new List<Newspaper> { item2 };

            await repo.Remove(itemsToRemove);

            var fileBytes = _mockFileSystem.File.ReadAllBytes(filePath);
            using var resultStream = new MemoryStream(fileBytes);

            var remainingItems = strategy.Deserialize<Newspaper>(resultStream).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(remainingItems.Count, Is.EqualTo(2), "Количество элементов должно уменьшиться");

                Assert.That(remainingItems.Any(i => i.Name == "Газета 2"), Is.False, "Газета 2 должна быть удалена");
                Assert.That(remainingItems.Any(i => i.Name == "Газета 1"), Is.True);
                Assert.That(remainingItems.Any(i => i.Name == "Газета 3"), Is.True);
            });
        }
    }
}

