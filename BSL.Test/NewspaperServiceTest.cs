using BSL.Implimentation;
using BSL.Models;
using FluentAssertions;
using Moq;
using static System.Reflection.Metadata.BlobBuilder;

namespace BSL.Test;

public class NewspaperServiceTest
{
    List<Newspaper> newspapers = new List<Newspaper>()
{
    new Newspaper(
        name: "Комсомольская правда",
        placeOfPublication: "Москва",
        publishingHouse: "ИД «Комсомольская правда»",
        numberOfPages: 16,
        notes: "Ежедневная общественно-политическая газета",
        issueNumber: 15430,
        dataPublishing: new DateOnly(2023, 10, 5),  // Дата выпуска (должна быть >= 1950)
        issn: "0233-4399"
    ),

    new Newspaper(
        name: "The New York Times",
        placeOfPublication: "Нью-Йорк, США",
        publishingHouse: "The New York Times Company",
        numberOfPages: 64,
        notes: null,                      // Пример nullable поля
        issueNumber: 58201,
        dataPublishing: new DateOnly(2003, 10, 5), // Еще один способ создания DateOnly
        issn: "0362-4331"
    ),

    new Newspaper(
        name: "Ведомости",
        placeOfPublication: "Москва",
        publishingHouse: "АО «Бизнес Ньюс Медиа»",
        numberOfPages: 32,
        notes: "Деловая газета",
        issueNumber: 450,
        dataPublishing: new DateOnly(1999, 9, 21),  // Минимально допустимый год по логике конструктора
        issn: "1562-2584"
    )
};
    private Mock<IRepository> GetRepositoryMoq<T>(List<T> res)
    {
        var repositoryMoq = new Mock<IRepository>();
        repositoryMoq.Setup(repos => repos.GetAll<T>()).Returns(res);
        return repositoryMoq;
    }

    [Test]
    public void GetAll_ReturnNewspaperList()
    {
        Mock<IRepository> repositoryMoq = GetRepositoryMoq<Newspaper>(newspapers);

        INewspaperService newspaperService = new NewspaperService(repositoryMoq.Object);
        IEnumerable<Newspaper> result = newspaperService.GetAll();
        result.Should().BeEquivalentTo(newspapers);

    }


    [Test]
    [TestCase(OrderBy.Asc)]
    [TestCase(OrderBy.Desc)]
    public void GetAll_ReturnNewspaperListAscOrderByYear(OrderBy orderBy)
    {
        INewspaperService bookService = new NewspaperService(GetRepositoryMoq(newspapers).Object);
        IEnumerable<Newspaper> result = bookService.GetAll(OrderBy.Asc);
        result.Should().BeEquivalentTo(orderBy == OrderBy.Asc
            ? newspapers.OrderBy(newspaper => newspaper.DataPublishing.Year)
            : newspapers.OrderByDescending(newspaper => newspaper.DataPublishing.Year));
    }
}
