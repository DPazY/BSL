using BSL.Models;
using BSL.Models.Enum;
using BSL.Models.Interface;

namespace BSL.Implementation.Service
{
    public class PatentService : EditionService, IPatentService
    {

        public PatentService(IRepository patentRepository): base(patentRepository) { }
        

        public async Task<IEnumerable<Patent>> GetAll(OrderBy? orderBy = null)
        {
            return orderBy switch
            {
                OrderBy.Asc => (await _editionRepository.GetAll<Patent>())
                .OrderBy(b => b.PublicationDate),
                OrderBy.Desc => (await _editionRepository.GetAll<Patent>())
                .OrderByDescending(b => b.PublicationDate),
                _ => await _editionRepository.GetAll<Patent>()
            };
        }
    }
}
