using BSL.Models;

namespace BSL.Implimentation
{
    public class EditionService : IEditionService
    {
        protected readonly IRepository _editionRepository;

        public EditionService(IRepository editionRepository)
        {
            this._editionRepository = editionRepository;
        }

        public IEnumerable<Edition> SearchByName(string name) => _editionRepository.GetAll<Edition>().Where(e => e.Name == name);

    }
}
