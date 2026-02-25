using System;
using System.Collections.Generic;
using System.Text;

namespace BSL.Models
{
    public interface ISerializerStrategy
    {
        void Serialize<T>(IEnumerable<T> valuess, Stream? stream = null);
        IEnumerable<T> Deserialize<T>(Stream stream);
    }
}
