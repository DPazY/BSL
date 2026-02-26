using BSL.Implementation;
using BSL.Models;
using FluentAssertions;
using Moq;
using System.Xml.Serialization;

namespace BSL.Test
{
    [TestFixture]
    public class BookXmlServiceTests
    {
        private Mock<IRepository> _repositoryMock;
        private BookXmlService _service;

        [SetUp]
        public void SetUp()
        {
            _repositoryMock = new Mock<IRepository>();
            _service = new BookXmlService(_repositoryMock.Object);
        }

        private Stream GetXmlStream(CatalogXmlDto dtos)
        {
            var ms = new MemoryStream();
            var serializer = new XmlSerializer(typeof(CatalogXmlDto));
            serializer.Serialize(ms, dtos);
            ms.Position = 0;
            return ms;
        }

        [Test]
        public void Import_ShouldAddNewBooks_WhenTheyDoNotExistInRepository()
        {
            var existingBook = new Book("Старая книга", new DateOnly(2000, 1, 1), "Издат", "Автор");
            _repositoryMock.Setup(r => r.GetAll<Book>()).Returns(new List<Book> { existingBook });

            var xmlBooks = new CatalogXmlDto()
            {
                Books = new List<BookXmlDto>()
                {
                    new BookXmlDto { Title = "Новая книга",
                    Author = "Новый Автор",
                    Publisher = "Прес",
                    PublishDate = "2020-05-05" }
                }
            };
            using var stream = GetXmlStream(xmlBooks);

            _service.Import(stream);

            _repositoryMock.Verify(r => r.Add(It.Is<IEnumerable<Book>>(books =>
                books.Count() == 2 &&
                books.Any(b => b.Name == "Новая книга") &&
                books.Any(b => b.Name == "Старая книга"))),
                Times.Once);
        }

        [Test]
        public void Import_ShouldUseDefaultValues_WhenXmlFieldsAreMissingOrInvalid()
        {
            _repositoryMock.Setup(r => r.GetAll<Book>()).Returns(new List<Book>());

            var xmlBooks = new CatalogXmlDto()
            {
                Date = "некорректная дата",
                Books = new List<BookXmlDto>
                {
                    new BookXmlDto { Title = null, Author = null, Publisher = null, PublishDate = "некорректная дата" }
                }

            };
            using var stream = GetXmlStream(xmlBooks);

            _service.Import(stream);

            _repositoryMock.Verify(r => r.Add(It.Is<IEnumerable<Book>>(books =>
                books.First().Name == "Неизвестное название" &&
                books.First().Author.Contains("Неизвестный") &&
                books.First().PublisherBook == "Неизвестное издательство" &&
                books.First().YearBook == 1900)),
                Times.Once);
        }

        [Test]
        public void Import_ShouldNotAddNewBooks_WhenTheyAlreadyExistInRepository()
        {
            var existingBook = new Book("Идиот", new DateOnly(2000, 1, 1), "АСТ", "Достоевский");
            _repositoryMock.Setup(r => r.GetAll<Book>()).Returns(new List<Book> { existingBook });

            var xmlBooks = new CatalogXmlDto()
            {
                Books = new List<BookXmlDto>()
                {
                    new BookXmlDto { Title = "Идиот", Author = "Федор Достоевский" }
                }
            };
            using var stream = GetXmlStream(xmlBooks);

            _service.Import(stream);

            _repositoryMock.Verify(r => r.Add(It.IsAny<IEnumerable<Book>>()), Times.Never);
        }

        [Test]
        public void Import_ShouldIgnoreDuplicates_WithinTheXmlStream()
        {
            _repositoryMock.Setup(r => r.GetAll<Book>()).Returns(new List<Book>());

            var xmlBooks = new CatalogXmlDto
            {
                Books = new List<BookXmlDto>()
                {
                new BookXmlDto { Title = "Уникальная книга" },
                new BookXmlDto { Title = "Уникальная книга" }
                }
            };
            using var stream = GetXmlStream(xmlBooks);

            _service.Import(stream);

            _repositoryMock.Verify(r => r.Add(It.Is<IEnumerable<Book>>(books => books.Count() == 1)), Times.Once);
        }

        [Test]
        public void Import_ShouldNotCallAdd_WhenNoNewBooksAreFound()
        {
            _repositoryMock.Setup(r => r.GetAll<Book>()).Returns(new List<Book>());

            var xmlBooks = new CatalogXmlDto();
            using var stream = GetXmlStream(xmlBooks);

            _service.Import(stream);

            _repositoryMock.Verify(r => r.Add(It.IsAny<IEnumerable<Book>>()), Times.Never);
        }

        [Test]
        public void Import_IntegrationTest_ShouldCorrectlyParseRealBooksXml()
        {
            var repositoryMock = new Mock<IRepository>();
            repositoryMock.Setup(r => r.GetAll<Book>()).Returns(new List<Book>());

            var service = new BookXmlService(repositoryMock.Object);

            string filePath = "books.xml";

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                service.Import(stream);
            }

            repositoryMock.Verify(r => r.Add(It.Is<IEnumerable<Book>>(books =>
                books.Count() == 12 &&
                books.Any(b => b.Name == "Midnight Rain")
            )), Times.Once);
        }

        [Test]
        public void Export_NullStream_ShouldThrowArgumentNullException()
        {
            Action act = () => _service.Export(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void Export_EmptyStream_ShouldWriteRepositoryBooksToStream()
        {
            var repoBooks = new List<Book>
            {
                new Book("Уникальная книга 1", new DateOnly(2020, 1, 1), "Издат", "Автор")
             };
            _repositoryMock.Setup(r => r.GetAll<Book>()).Returns(repoBooks);

            using var stream = new MemoryStream();

            _service.Export(stream);

            stream.Position = 0;
            var serializer = new XmlSerializer(typeof(CatalogXmlDto));
            var resultCatalog = (CatalogXmlDto)serializer.Deserialize(stream);

            resultCatalog.Should().NotBeNull();
            resultCatalog.Books.Should().HaveCount(1);
            resultCatalog.Books.First().Title.Should().Be("Уникальная книга 1");
            resultCatalog.Books.First().Author.Should().Be("Автор");
        }

        [Test]
        public void Export_ExistingXml_ShouldAppendOnlyNewBooks()
        {
            var initialXml = new CatalogXmlDto()
            {
                Books = new List<BookXmlDto>
                {
            new BookXmlDto { Title = "Существующая книга", Author = "Старый Автор" }
                }
            };
            using var stream = GetXmlStream(initialXml);

            var repoBooks = new List<Book>
            {
                new Book("Новая книга", new DateOnly(2021, 5, 5), "Прес", "Новый Автор")
            };
            _repositoryMock.Setup(r => r.GetAll<Book>()).Returns(repoBooks);

            _service.Export(stream);

            stream.Position = 0;
            var serializer = new XmlSerializer(typeof(CatalogXmlDto));
            var resultCatalog = (CatalogXmlDto)serializer.Deserialize(stream);

            resultCatalog.Books.Should().HaveCount(2, "В XML должны быть и старая, и новая книги");
            resultCatalog.Books.Should().ContainSingle(b => b.Title == "Существующая книга");
            resultCatalog.Books.Should().ContainSingle(b => b.Title == "Новая книга");
        }

        [Test]
        public void Export_ExistingXml_ShouldNotAppendDuplicateBooks()
        {
            var initialXml = new CatalogXmlDto()
            {
                Books = new List<BookXmlDto>
                {
                    new BookXmlDto { Title = "Дубликат", Author = "Автор" }
                }
            };
            using var stream = GetXmlStream(initialXml);

            var repoBooks = new List<Book>
            {
                new Book("Дубликат", new DateOnly(2021, 1, 1), "Прес", "Автор"),
                new Book("Уникальная книга", new DateOnly(2022, 2, 2), "Прес", "Автор 2")
            };
            _repositoryMock.Setup(r => r.GetAll<Book>()).Returns(repoBooks);

            _service.Export(stream);

            stream.Position = 0;
            var serializer = new XmlSerializer(typeof(CatalogXmlDto));
            var resultCatalog = (CatalogXmlDto)serializer.Deserialize(stream);

            resultCatalog.Books.Should().HaveCount(2, "Дубликат не должен быть добавлен дважды");
            resultCatalog.Books.Should().ContainSingle(b => b.Title == "Дубликат");
            resultCatalog.Books.Should().ContainSingle(b => b.Title == "Уникальная книга");
        }

        [Test]
        public void Export_IntegrationTest_ShouldCorrectlyAppendToRealBooksXml()
        {
            var repoBooks = new List<Book>
            {
            new Book("Midnight Rain", new DateOnly(2000, 12, 16), "R & D", "Ralls, Kim"), 
            new Book("CLR via C#", new DateOnly(2012, 1, 1), "Microsoft", "Jeffrey Richter") 
            };
            _repositoryMock.Setup(r => r.GetAll<Book>()).Returns(repoBooks);

            string filePath = "books.xml";
            byte[] fileBytes = File.ReadAllBytes(filePath);

            using var stream = new MemoryStream();
            stream.Write(fileBytes, 0, fileBytes.Length);
            stream.Position = 0;

            _service.Export(stream);

            stream.Position = 0;
            var serializer = new XmlSerializer(typeof(CatalogXmlDto));
            var resultCatalog = (CatalogXmlDto)serializer.Deserialize(stream);

            resultCatalog.Books.Should().HaveCount(13);
            resultCatalog.Books.Should().ContainSingle(b => b.Title == "Midnight Rain", "Оригинальная книга должна остаться");
            resultCatalog.Books.Should().ContainSingle(b => b.Title == "CLR via C#", "Новая книга должна быть добавлена");
        }
    }
}