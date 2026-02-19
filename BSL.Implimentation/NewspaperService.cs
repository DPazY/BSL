using BSL.Models;

namespace BSL.Implimentation
{
    public class NewspaperService : INewspaperService
    {
        private readonly IRepository _newspaperRepository;

        public NewspaperService(IRepository newspaperRepository)
        {
            _newspaperRepository = newspaperRepository;
        }

        public IEnumerable<Newspaper> GetAll(OrderBy? orderBy = null)
        {
            return orderBy switch
            {
                OrderBy.Asc => _newspaperRepository.GetAll<Newspaper>().OrderBy(b => b.DataPublishing.Year),
                OrderBy.Desc => _newspaperRepository.GetAll<Newspaper>().OrderByDescending(b => b.DataPublishing.Year),
                _ => _newspaperRepository.GetAll<Newspaper>()
            };
        }
    }
}
