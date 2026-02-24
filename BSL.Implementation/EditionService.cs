using BSL.Models;

namespace BSL.Implementation
{
    public class EditionService : IEditionService
    {
        protected readonly IRepository _editionRepository;

        public EditionService(IRepository editionRepository)
        {
            this._editionRepository = editionRepository;
        }

        public IEnumerable<Edition> SearchByName(string name)
        {
            return _editionRepository.GetAll<Book>()
                .Cast<Edition>()
                .Concat(_editionRepository.GetAll<Newspaper>()) 
                .Concat(_editionRepository.GetAll<Patent>())
                .Where(e => e.Name == name);
        }

    }
}
