using BSL.Models;

namespace BSL.Implimentation
{
    public class PatentService : IPatentService
    {
        private readonly IRepository _patentRepository;

        public PatentService(IRepository patentRepository)
        {
            _patentRepository = patentRepository;
        }

        public IEnumerable<Patent> GetAll(OrderBy? orderBy = null)
        {
            return orderBy switch
            {
                OrderBy.Asc => _patentRepository.GetAll<Patent>().OrderBy(b => b.PublicationDate),
                OrderBy.Desc => _patentRepository.GetAll<Patent>().OrderByDescending(b => b.PublicationDate),
                _ => _patentRepository.GetAll<Patent>()
            };
        }
    }
}
