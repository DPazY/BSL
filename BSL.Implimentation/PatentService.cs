using BSL.Models;

namespace BSL.Implimentation
{
    public class PatentService
    {
        private readonly IRepository<Patent> _patentRepository;

        public PatentService(IRepository<Patent> patentRepository)
        {
            _patentRepository = patentRepository;
        }

        public IEnumerable<Patent> GetAll(OrderBy? orderBy = null)
        {
            return orderBy switch
            {
                OrderBy.Asc => _patentRepository.GetAll().OrderBy(b => b.PublicationDate),
                OrderBy.Desc => _patentRepository.GetAll().OrderByDescending(b => b.PublicationDate),
                _ => _patentRepository.GetAll()
            };
        }
    }
}
