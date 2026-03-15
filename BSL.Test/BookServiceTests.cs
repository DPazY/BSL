using BSL.Implementation.Service;
using BSL.Models;
using BSL.Models.Enum;
using BSL.Models.Interface;
using FluentAssertions;
using Moq;

namespace BSL.Test
{
    public class BookServiceTests
    {
        private readonly List<Book> books =
            [
                new("Преступление и наказание", new DateOnly(2000, 1, 1), "БББ", "Толстой"),
                new("Идиот", new DateOnly(2003, 1, 1), "аба", "Толстой Мартин"),
                new("Песнь Льда и Пламени", new DateOnly(1978, 1, 1), "ААА", "Мартин")
            ];
        private Mock<IRepository> GetRepositoryMoq<T>(List<T> res)
        {
            var repositoryMoq = new Mock<IRepository>();
            repositoryMoq.Setup(repos => repos.GetAll<T>()).Returns(res);
            return repositoryMoq;
        }

        [Test]
        public void GetAll_ReturnBookList()
        {
            Mock<IRepository> repositoryMoq = GetRepositoryMoq(books);

            IBookService bookService = new BookService(repositoryMoq.Object);
            IEnumerable<Book> result = bookService.GetAll();
            result.Should().BeEquivalentTo(books);

        }


        [Test]
        [TestCase(OrderBy.Asc)]
        [TestCase(OrderBy.Desc)]
        public void GetAll_ReturnBookListAscOrderByYear(OrderBy orderBy)
        {
            IBookService bookService = new BookService(GetRepositoryMoq(books).Object);
            IEnumerable<Book> result = bookService.GetAll(OrderBy.Asc);
            result.Should().BeEquivalentTo(orderBy == OrderBy.Asc
                ? books.OrderBy(book => book.YearBook)
                : books.OrderByDescending(book => book.YearBook));
        }

        [Test]
        public void GetAllWherePublisherStarts_ReturnBookListStartWithPatternOrderByAsc()
        {
            IBookService bookService = new BookService(GetRepositoryMoq(books).Object);
            IEnumerable<Book> result1 = bookService.GetAllWherePublisherStarts("а");
            IEnumerable<Book> result2 = bookService.GetAllWherePublisherStarts("аб");

            result1.Should().BeEquivalentTo([books[2], books[1]]);
            result2.Should().BeEquivalentTo([books[1]]);
        }
        [Test]
        public void GetAllAuthors_ReturnBookList()
        {
            IBookService bookService = new BookService(GetRepositoryMoq(books).Object);
            IEnumerable<Book> result1 = bookService.GetAllByAuthor("Толстой");
            IEnumerable<Book> result2 = bookService.GetAllByAuthor("Мартин");

            result1.Should().BeEquivalentTo([books[0], books[1]]);
            result2.Should().BeEquivalentTo([books[1], books[2]]);
        }
    }
}
