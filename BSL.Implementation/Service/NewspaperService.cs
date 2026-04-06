using BSL.Models;
using BSL.Models.Enum;
using BSL.Models.Interface;

namespace BSL.Implementation.Service
{
    public class NewspaperService :EditionService, INewspaperService
    {
        public NewspaperService(IRepository newspaperRepository): base(newspaperRepository) { }

        public async Task<IEnumerable<Newspaper>> GetAll(OrderBy? orderBy = null)
        {
            return orderBy switch
            {
                OrderBy.Asc => (await _editionRepository.GetAll<Newspaper>())
                .OrderBy(b => b.DataPublishing.Year),
                OrderBy.Desc => (await _editionRepository.GetAll<Newspaper>())
                .OrderByDescending(b => b.DataPublishing.Year),
                _ => await _editionRepository.GetAll<Newspaper>()
            };
        }
    }
}
