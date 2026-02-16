using BSL.Implimentation;
using BSL.Models;
using FluentAssertions;
using Moq;

namespace BSL.Test;

public class PatentServiceTest
{
    List<Patent> patents = new List<Patent>()
    {
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
    private Mock<IRepository<T>> GetPatentRepositoryMoq<T>(List<T> res)
    {
        var repositoryMoq = new Mock<IRepository<T>>();
        repositoryMoq.Setup(repos => repos.GetAll()).Returns(res);
        return repositoryMoq;
    }

    [Test]
    public void GetAll_ReturnBookList()
    {
        Mock<IRepository<Patent>> repositoryMoq = GetPatentRepositoryMoq<Patent>(patents);

        IPatentService patentService = new PatentService(repositoryMoq.Object);
        IEnumerable<Patent> result = patentService.GetAll();
        result.Should().BeEquivalentTo(patents);

    }


    [Test]
    [TestCase(OrderBy.Asc)]
    [TestCase(OrderBy.Desc)]
    public void GetAll_ReturnBookListAscOrderByYear(OrderBy orderBy)
    {
        IPatentService patentService = new PatentService(GetPatentRepositoryMoq<Patent>(patents).Object);
        IEnumerable<Patent> result = patentService.GetAll(OrderBy.Asc);
        result.Should().BeEquivalentTo(orderBy == OrderBy.Asc
            ? patents.OrderBy(newspaper => newspaper.PublicationDate.Year)
            : patents.OrderByDescending(newspaper => newspaper.PublicationDate.Year));
    }
}
