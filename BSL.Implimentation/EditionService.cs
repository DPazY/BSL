using BSL.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSL.Implimentation
{
    public class EditionService : IEditionService
    {
        private readonly IRepository<Editions> _editionRepository;

        public EditionService(IRepository<Editions> editionRepository)
        {
            this._editionRepository = editionRepository;
        }

        public IEnumerable<Editions> SearchByName(string name) => _editionRepository.GetAll().Where(e => e.Name == name);
        
    }
}
