using BSL.Models;
using System.Xml.Serialization;

namespace BSL.Implementation
{
    public class XmlSerializerStrategy : ISerializerStrategy
    {
        public IEnumerable<T> Deserialize<T>(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream, nameof(stream));

            XmlSerializer _serializer = new XmlSerializer(typeof(List<T>));
            return (IEnumerable<T>)_serializer.Deserialize(stream);
        }

        public void Serialize<T>(IEnumerable<T> values, Stream? stream = null)
        {
            ArgumentNullException.ThrowIfNull(stream, nameof(stream));

            XmlSerializer _serializer = new XmlSerializer(typeof(List<T>));
            _serializer.Serialize(stream, values.ToList());
        }
    }
}
