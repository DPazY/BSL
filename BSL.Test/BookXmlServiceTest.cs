using BSL.Implementation;
using BSL.Models;
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
    }
}