using BSL.Implimentation;
using BSL.Models;
using FluentAssertions;
using Moq;

namespace BSL.Test;

public class EditionsServiceTest
{
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
    private Mock<IRepository> GetRepositoryMoq<T>(List<T> res)
    {
        var repositoryMoq = new Mock<IRepository>();
        repositoryMoq.Setup(repos => repos.GetAll<T>()).Returns(res);
        return repositoryMoq;
    }

    [Test]
    [TestCase("Идиот")]
    [TestCase("Песнь Льда и Пламени")]
    [TestCase("The New York Times")]


    public void SearchByName_ReturnEditionList(string name)
    {
        IEditionService editionService = new EditionService(GetRepositoryMoq<Edition>(editions).Object);
        IEnumerable<Edition> result = editionService.SearchByName(name);

        result.Should().BeEquivalentTo(editions.Where(b => b.Name == name));
    }

}
