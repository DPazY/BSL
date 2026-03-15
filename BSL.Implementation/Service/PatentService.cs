using BSL.Models;
using BSL.Models.Enum;
using BSL.Models.Interface;

namespace BSL.Implementation.Service
{
    public class PatentService : EditionService, IPatentService
    {

        public PatentService(IRepository patentRepository): base(patentRepository) { }
        

        public IEnumerable<Patent> GetAll(OrderBy? orderBy = null)
        {
            return orderBy switch
            {
                OrderBy.Asc => _editionRepository.GetAll<Patent>().OrderBy(b => b.PublicationDate),
                OrderBy.Desc => _editionRepository.GetAll<Patent>().OrderByDescending(b => b.PublicationDate),
                _ => _editionRepository.GetAll<Patent>()
            };
        }
    }
}
