using BSL.Models;
using ProtoBuf;

namespace BSL.Implementation
{
    public class ProtobufSerializerStrategy : ISerializerStrategy
    {
        public IEnumerable<T> Deserialize<T>(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream, nameof(stream));

            return Serializer.DeserializeItems<T>(stream, PrefixStyle.Base128, 1).ToList();
        }

        public void Serialize<T>(IEnumerable<T> values, Stream? stream = null)
        {
            ArgumentNullException.ThrowIfNull(stream, nameof(stream));
            foreach (var item in values)
            {
                Serializer.SerializeWithLengthPrefix<T>(stream, item, PrefixStyle.Base128, 1);
            }
        }
    }
}
