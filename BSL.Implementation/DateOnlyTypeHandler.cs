using Dapper;
using System.Data;

namespace BSL.Implementation
{
    public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly date)
        {
            parameter.DbType = DbType.Date;

            parameter.Value = date;
        }

        public override DateOnly Parse(object value)
        {
            if (value is DateOnly dateOnly)
                return dateOnly;

            if (value is DateTime dateTime)
                return DateOnly.FromDateTime(dateTime);

            throw new InvalidCastException($"Невозможно преобразовать тип {value.GetType()} в DateOnly");
        }
    }
}