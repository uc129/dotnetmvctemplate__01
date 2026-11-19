using System.Data;
using Dapper;

namespace AppTemplate.Infrastructure.Data;

#pragma warning disable RS0030 // Registered with Dapper by DapperContext; this is part of the data-access boundary.

/// <summary>
/// Dapper reads SQL DATE as DateTime and has no built-in DateOnly mapping. This handler sends
/// DateOnly as a SQL DATE parameter and reads DATE columns into DateOnly properties.
/// Limitation: Dapper applies type handlers to properties, not constructor parameters, so row
/// types that hold a DateOnly are classes with init properties, not positional records.
/// </summary>
internal sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value) => value switch
    {
        DateOnly date => date,
        DateTime dateTime => DateOnly.FromDateTime(dateTime),
        _ => throw new DataException($"Cannot convert {value.GetType().Name} to DateOnly."),
    };
}
#pragma warning restore RS0030
