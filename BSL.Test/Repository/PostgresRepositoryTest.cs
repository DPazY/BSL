using BSL.Implementation;
using BSL.Implementation.Repository;
using BSL.Models;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Transactions;

namespace BSL.Test.Repository
{
    [TestFixture]
    public class PostgresRepositoryTest
    {
        private string _testConnectionString;

        private PostgresRepository _repository;
        private TransactionScope _transactionScope;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var config = new ConfigurationBuilder()
                .AddUserSecrets<PostgresRepositoryTest>()
                .Build();

            _testConnectionString = config["TestConnectionString"];

            if (string.IsNullOrEmpty(_testConnectionString))
            {
                throw new InvalidOperationException("Секрет 'TestConnectionString' не найден. Убедитесь, что выполнили dotnet user-secrets set.");
            }

            SqlMapper.AddTypeHandler(new StringListTypeHandler());
            SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

            using var db = new NpgsqlConnection(_testConnectionString);
            db.Open();
            db.Execute(@"
                CREATE TABLE IF NOT EXISTS Books (
                    Name VARCHAR(255) PRIMARY KEY,
                    YearBook INT NOT NULL,
                    PublisherBook VARCHAR(255) NOT NULL,
                    Author TEXT[] NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Newspapers (
                    Name VARCHAR(255) PRIMARY KEY,
                    PlaceOfPublication VARCHAR(255),
                    PublishingHouse VARCHAR(255) NOT NULL,
                    NumberOfPages INT NOT NULL,
                    Notes TEXT,
                    IssueNumber INT NOT NULL,
                    DataPublishing DATE NOT NULL,
                    ISSN VARCHAR(50)
                );
            ");
        }

        [SetUp]
        public void SetUp()
        {
            _repository = new PostgresRepository(_testConnectionString);

            _transactionScope = new TransactionScope(
                TransactionScopeOption.Required,
                TransactionScopeAsyncFlowOption.Enabled);
        }

        [TearDown]
        public void TearDown()
        {
            _transactionScope?.Dispose();
        }

        [Test]
        public void Add_ShouldInsertBooksIntoDatabase()
        {
            var books = new List<Book>
            {
                new Book("CLR via C#", new DateOnly(2012, 1, 1), "Microsoft Press", "Jeffrey Richter"),
                new Book("C# in Depth", new DateOnly(2019, 1, 1), "Manning", "Jon Skeet")
            };

            _repository.Add(books);

            var result = _repository.GetAll<Book>().ToList();

            result.Should().HaveCount(2);
            result.Should().ContainSingle(b => b.Name == "CLR via C#");
            result.Should().ContainSingle(b => b.Name == "C# in Depth");

            var skeetBook = result.First(b => b.Name == "C# in Depth");
            skeetBook.PublisherBook.Should().Be("Manning");
            skeetBook.YearBook.Should().Be(2019);
            skeetBook.Author.Should().Contain("Jon Skeet");
        }

        [Test]
        public void Add_DuplicateName_ShouldNotThrowExceptionDueToOnConflict()
        {
            var book1 = new Book("Паттерны проектирования", new DateOnly(2000, 1, 1), "Питер", "Банда Четырех");
            var book2 = new Book("Паттерны проектирования", new DateOnly(2022, 1, 1), "Новое Издательство", "Новый Автор");

            _repository.Add(new[] { book1 });

            Action act = () => _repository.Add(new[] { book2 });

            act.Should().NotThrow("потому что в репозитории используется ON CONFLICT (Name) DO NOTHING");

            var result = _repository.GetAll<Book>().ToList();
            result.Should().HaveCount(1, "дубликат не должен был добавиться");
            result.First().YearBook.Should().Be(2000, "должна остаться первоначальная версия книги");
        }

        [Test]
        public void GetByName_WhenItemExists_ShouldReturnItem()
        {
            var book = new Book("Оптимизация .NET", new DateOnly(2023, 5, 5), "БХВ", "Саша Голдштейн");
            _repository.Add(new[] { book });

            var result = _repository.GetByName<Book>("Оптимизация .NET");

            result.Should().NotBeNull();
            result!.Name.Should().Be("Оптимизация .NET");
            result.PublisherBook.Should().Be("БХВ");
        }

        [Test]
        public void GetByName_WhenItemDoesNotExist_ShouldReturnNull()
        {
            var result = _repository.GetByName<Book>("Несуществующая книга");

            result.Should().BeNull();
        }

        [Test]
        public void Remove_ShouldDeleteSpecificItemsFromDatabase()
        {
            var book1 = new Book("Книга на удаление", new DateOnly(2020, 1, 1), "Издат", "Автор 1");
            var book2 = new Book("Книга останется", new DateOnly(2021, 1, 1), "Издат", "Автор 2");

            _repository.Add(new[] { book1, book2 });

            _repository.Remove(new[] { book1 });

            var result = _repository.GetAll<Book>().ToList();
            result.Should().HaveCount(1);
            result.Should().ContainSingle(b => b.Name == "Книга останется");
            result.Should().NotContain(b => b.Name == "Книга на удаление");
        }

        [Test]
        public void AddAndGetAll_ShouldWorkForNewspapers()
        {
            var newspapers = new List<Newspaper>
            {
                new Newspaper(
                    name: "The Times",
                    placeOfPublication: "London",
                    publishingHouse: "News UK",
                    numberOfPages: 48,
                    notes: "Daily paper",
                    issueNumber: 73000,
                    dataPublishing: new DateOnly(2023, 11, 15),
                    issn: "0140-0460"
                )
            };

            _repository.Add(newspapers);
            var result = _repository.GetAll<Newspaper>().ToList();

            result.Should().HaveCount(1);

            var dbNewspaper = result.First();
            dbNewspaper.Name.Should().Be("The Times");
            dbNewspaper.PlaceOfPublication.Should().Be("London");
            dbNewspaper.PublishingHouse.Should().Be("News UK");
            dbNewspaper.NumberOfPages.Should().Be(48);
            dbNewspaper.IssueNumber.Should().Be(73000);
            dbNewspaper.ISSN.Should().Be("0140-0460");
        }
    }
}