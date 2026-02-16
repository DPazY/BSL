using BSL.Implimentation;
using BSL.Models;
using FluentAssertions;
using Moq;

namespace BSL.Test;

public class NewspaperServiceTest
{
    List<Newspaper> newspapers = new List<Newspaper>()
{
    new Newspaper(
        name: "Комсомольская правда",
        placeOfPublication: "Москва",
        publishingHouse: "ИД «Комсомольская правда»",
        yearOfPublication: 1925,          // Год основания газеты (может быть любым)
        numberOfPages: 16,
        notes: "Ежедневная общественно-политическая газета",
        issueNumber: 15430,
        date: new DateOnly(2023, 10, 5),  // Дата выпуска (должна быть >= 1950)
        iSSN: "0233-4399"
    ),

    new Newspaper(
        name: "The New York Times",
        placeOfPublication: "Нью-Йорк, США",
        publishingHouse: "The New York Times Company",
        yearOfPublication: 1851,
        numberOfPages: 64,
        notes: null,                      // Пример nullable поля
        issueNumber: 58201,
        date: DateOnly.FromDateTime(DateTime.Today), // Еще один способ создания DateOnly
        iSSN: "0362-4331"
    ),

    new Newspaper(
        name: "Ведомости",
        placeOfPublication: "Москва",
        publishingHouse: "АО «Бизнес Ньюс Медиа»",
        yearOfPublication: 1999,
        numberOfPages: 32,
        notes: "Деловая газета",
        issueNumber: 450,
        date: new DateOnly(1999, 9, 21),  // Минимально допустимый год по логике конструктора
        iSSN: "1562-2584"
    )
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
        Mock<IRepository<Newspaper>> repositoryMoq = GetBookRepositoryMoq<Newspaper>(newspapers);

        INewspaperService newspaperService = new NewspaperService(repositoryMoq.Object);
        IEnumerable<Newspaper> result = newspaperService.GetAll();
        result.Should().BeEquivalentTo(newspapers);

    }


    [Test]
    [TestCase(OrderBy.Asc)]
    [TestCase(OrderBy.Desc)]
    public void GetAll_ReturnBookListAscOrderByYear(OrderBy orderBy)
    {
        INewspaperService bookService = new NewspaperService(GetBookRepositoryMoq(newspapers).Object);
        IEnumerable<Newspaper> result = bookService.GetAll(OrderBy.Asc);
        result.Should().BeEquivalentTo(orderBy == OrderBy.Asc
            ? newspapers.OrderBy(newspaper => newspaper.DataPublishing.Year)
            : newspapers.OrderByDescending(newspaper => newspaper.DataPublishing.Year));
    }
}
