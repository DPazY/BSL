using BSL.Models;
using BSL.Models.Interface;

namespace BSL.Implementation.Service
{
    public class EditionService : IEditionService
    {
        protected readonly IRepository _editionRepository;

        public EditionService(IRepository editionRepository)
        {
            this._editionRepository = editionRepository;
        }

        public async Task<IEnumerable<Edition>> SearchByName(string name)
        {
            return (await _editionRepository.GetAll<Book>())
                .Cast<Edition>()
                .Concat(await _editionRepository.GetAll<Newspaper>()) 
                .Concat(await _editionRepository.GetAll<Patent>())
                .Where(e => e.Name == name);
        }

    }
}
