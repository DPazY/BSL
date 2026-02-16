using BSL.Implimentation;
using BSL.Models;
using FluentAssertions;
using Moq;
using System.Text.RegularExpressions;

namespace BSL.Test
{
    public class BookServiceTests
    {
        List<Book> books = new List<Book>()
            {
                new("Преступление и наказание", new DateOnly(2000, 1, 1), "БББ", "Толстой"),
                new("Идиот", new DateOnly(2003, 1, 1), "аба", "Толстой Мартин"),
                new("Песнь Льда и Пламени", new DateOnly(1978, 1, 1), "ААА", "Мартин")
            };
        private Mock<IRepository<T>> GetBookRepositoryMoq<T>(List<T> res)
        {
            var repositoryMoq = new Mock<IRepository<T>>();
            repositoryMoq.Setup(repos => repos.GetAll()).Returns(res);
            return repositoryMoq;
        }

        [Test]
        public void GetAll_ReturnBookList()
        {
            Mock<IRepository<Book>> repositoryMoq = GetBookRepositoryMoq(books);

            IBookService bookService = new BookService(repositoryMoq.Object);
            IEnumerable<Book> result = bookService.GetAll();
            result.Should().BeEquivalentTo(books);

        }


        [Test]
        [TestCase(OrderBy.Asc)]
        [TestCase(OrderBy.Desc)]
        public void GetAll_ReturnBookListAscOrderByYear(OrderBy orderBy)
        {
            IBookService bookService = new BookService(GetBookRepositoryMoq(books).Object);
            IEnumerable<Book> result = bookService.GetAll(OrderBy.Asc);
            result.Should().BeEquivalentTo(orderBy == OrderBy.Asc
                ? books.OrderBy(book => book.YearBook)
                : books.OrderByDescending(book => book.YearBook));
        }

        [Test]
        public void GetAllWherePublisherStarts_ReturnBookListStartWithPatternOrderByAsc()
        {
            IBookService bookService = new BookService(GetBookRepositoryMoq(books).Object);
            IEnumerable<Book> result1 = bookService.GetAllWherePublisherStarts("а");
            IEnumerable<Book> result2 = bookService.GetAllWherePublisherStarts("аб");

            result1.Should().BeEquivalentTo(new[] { books[2], books[1] });
            result2.Should().BeEquivalentTo(new[] { books[1] });
        }
        [Test]
        public void GetAllAuthors_ReturnBookList()
        {
            IBookService bookService = new BookService(GetBookRepositoryMoq(books).Object);
            IEnumerable<Book> result1 = bookService.GetAllByAuthor("Толстой");
            IEnumerable<Book> result2 = bookService.GetAllByAuthor("Мартин");

            result1.Should().BeEquivalentTo(new[] { books[0], books[1] });
            result2.Should().BeEquivalentTo(new[] { books[1], books[2] });
        }
    }
}
