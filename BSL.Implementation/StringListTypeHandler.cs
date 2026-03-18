using Dapper;
using System.Data;

namespace BSL.Implementation
{
    public class StringListTypeHandler : SqlMapper.TypeHandler<List<string>>
    {
        public override void SetValue(IDbDataParameter parameter, List<string> value)
        {
            parameter.Value = value.ToArray();
        }

        public override List<string> Parse(object value)
        {
            if (value is string[] array)
            {
                return array.ToList();
            }
            return new List<string>();
        }
    }
}