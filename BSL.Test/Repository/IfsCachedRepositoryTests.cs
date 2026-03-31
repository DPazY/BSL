using BSL.Implementation.Repository;
using BSL.Models;
using BSL.Models.Interface;
using FluentAssertions;
using Moq;

namespace BSL.Test.Repository
{
    [TestFixture]
    public class IfsCachedRepositoryTest
    {
        private Mock<IRepository> _innerRepositoryMock;
        private Mock<ITelemetryAggregator> _telemetryAggregatorMock;
        private IfsCachedRepository _repository;

        private const long MaxMemoryBytes = 10 * 1024 * 1024;

        [SetUp]
        public void SetUp()
        {
            _innerRepositoryMock = new Mock<IRepository>();
            _telemetryAggregatorMock = new Mock<ITelemetryAggregator>();

            _repository = new IfsCachedRepository(
                _innerRepositoryMock.Object,
                _telemetryAggregatorMock.Object,
                null,
                MaxMemoryBytes);
        }

        [Test]
        public void GetByName_CacheMiss_FetchesFromInnerRepository_And_RecordsTelemetry()
        {
            var bookName = "CLR via C#";
            var expectedCompositeKey = $"Book:{bookName}";
            var expectedBook = new Book(bookName, new DateOnly(2012, 1, 1), "Microsoft Press", "Jeffrey Richter");

            _innerRepositoryMock
                .Setup(r => r.GetByName<Book>(bookName))
                .Returns(expectedBook);

            var result = _repository.GetByName<Book>(bookName);

            result.Should().NotBeNull();
            result!.Name.Should().Be(bookName);
            result.Author.Should().Contain("Jeffrey Richter");

            _innerRepositoryMock.Verify(r => r.GetByName<Book>(bookName), Times.Once);

            _telemetryAggregatorMock.Verify(t => t.RecordHit(expectedCompositeKey), Times.Once);
        }

        [Test]
        public void GetByName_CacheHit_ReturnsFromMemory_And_DoesNotCallInnerRepositoryTwice()
        {
            var bookName = "C# in Depth";
            var expectedCompositeKey = $"Book:{bookName}";
            var expectedBook = new Book(bookName, new DateOnly(2019, 1, 1), "Manning", "Jon Skeet");

            _innerRepositoryMock
                .Setup(r => r.GetByName<Book>(bookName))
                .Returns(expectedBook);

            var firstCallResult = _repository.GetByName<Book>(bookName);

            var secondCallResult = _repository.GetByName<Book>(bookName);

            firstCallResult.Should().NotBeNull();
            secondCallResult.Should().NotBeNull();

            firstCallResult.Should().BeSameAs(secondCallResult);

            _innerRepositoryMock.Verify(r => r.GetByName<Book>(bookName), Times.Once,
                "потому что второй запрос должен был отработать из кэша");

            // Телеметрия зафиксировала оба обращения (для расчета IFS)
            _telemetryAggregatorMock.Verify(t => t.RecordHit(expectedCompositeKey), Times.Exactly(2),
                "потому что телеметрия должна собираться при каждом обращении, независимо от попадания в кэш");
        }

        [Test]
        public void GetByName_WhenItemDoesNotExistInDb_ShouldReturnNull_And_NotCache()
        {
            var bookName = "Несуществующая книга";
            var expectedCompositeKey = $"Book:{bookName}";

            _innerRepositoryMock
                .Setup(r => r.GetByName<Book>(bookName))
                .Returns((Book)null); 

            var firstCallResult = _repository.GetByName<Book>(bookName);
            var secondCallResult = _repository.GetByName<Book>(bookName);

            firstCallResult.Should().BeNull();
            secondCallResult.Should().BeNull();

            _innerRepositoryMock.Verify(r => r.GetByName<Book>(bookName), Times.Exactly(2));

            _telemetryAggregatorMock.Verify(t => t.RecordHit(expectedCompositeKey), Times.Exactly(2));
        }
    }
}