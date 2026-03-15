using BSL.Models;
using BSL.Models.Enum;
using BSL.Models.Interface;

namespace BSL.Implementation.Service
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
