using ProtoBuf;

namespace BSL.Models
{
    [ProtoInclude(100, typeof(Book))]
    [ProtoInclude(200, typeof(Newspaper))]
    [ProtoInclude(300, typeof(Patent))]
    [ProtoContract]
    public abstract record Edition
    {
        protected Edition(string name)
        {
            Name = name;
        }
        [ProtoMember(1)]
        public required string Name { get; set; }
    }
}