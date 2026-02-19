using BSL.Models;

namespace BSL.Implimentation
{
    public class NewspaperService :EditionService, INewspaperService
    {
        public NewspaperService(IRepository newspaperRepository): base(newspaperRepository) { }

        public IEnumerable<Newspaper> GetAll(OrderBy? orderBy = null)
        {
            return orderBy switch
            {
                OrderBy.Asc => _editionRepository.GetAll<Newspaper>().OrderBy(b => b.DataPublishing.Year),
                OrderBy.Desc => _editionRepository.GetAll<Newspaper>().OrderByDescending(b => b.DataPublishing.Year),
                _ => _editionRepository.GetAll<Newspaper>()
            };
        }
    }
}
