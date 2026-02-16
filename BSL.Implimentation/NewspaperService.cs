using BSL.Models;

namespace BSL.Implimentation
{
    public class NewspaperService : INewspaperService
    {
        private readonly IRepository<Newspaper> _newspaperRepository;

        public NewspaperService(IRepository<Newspaper> newspaperRepository)
        {
            _newspaperRepository = newspaperRepository;
        }

        public IEnumerable<Newspaper> GetAll(OrderBy? orderBy = null)
        {
            return orderBy switch
            {
                OrderBy.Asc => _newspaperRepository.GetAll().OrderBy(b => b.DataPublishing.Year),
                OrderBy.Desc => _newspaperRepository.GetAll().OrderByDescending(b => b.DataPublishing.Year),
                _ => _newspaperRepository.GetAll()
            };
        }

        public IEnumerable<Newspaper> SearchByName(string name)
        {
            return _newspaperRepository.GetAll().Where(b => b.Name == name);
        }
    }
}
