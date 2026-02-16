using System;
using System.Collections.Generic;

namespace BSL.Models
{
    public interface IPatentService
    {
        public IEnumerable<Patent> GetAll(OrderBy? orderBy = null);
        bool Add(Patent patent);
        bool Remove(Patent patent);
        IEnumerable<Patent> SearchByName(string name);
    }
}