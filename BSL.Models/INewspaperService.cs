using System;
using System.Collections.Generic;

namespace BSL.Models
{
    public interface INewspaperService
    {
        public IEnumerable<Newspaper> GetAll(OrderBy? orderBy = null);
        bool Add(Newspaper newspaper);
        bool Remove(Newspaper newspaper);
        IEnumerable<Newspaper> SearchByName(string name);
    }
}