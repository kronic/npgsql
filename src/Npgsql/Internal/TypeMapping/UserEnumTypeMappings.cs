using System;
using System.Collections.Generic;
using System.Reflection;
using Npgsql.Internal.TypeHandlers;
using Npgsql.Internal.TypeHandling;
using Npgsql.PostgresTypes;
using NpgsqlTypes;

namespace Npgsql.Internal.TypeMapping;

public interface IUserEnumTypeMapping : IUserTypeMapping
{
    INpgsqlNameTranslator NameTranslator { get; }
}

sealed class UserEnumTypeMapping<TEnum> : IUserEnumTypeMapping
    where TEnum : struct, Enum
{
    public string PgTypeName { get; }
    public Type ClrType => typeof(TEnum);
    public INpgsqlNameTranslator NameTranslator { get; }

    readonly Dictionary<TEnum, string> _enumToLabel;
    readonly Dictionary<string, TEnum> _labelToEnum;

    public UserEnumTypeMapping(string pgTypeName, INpgsqlNameTranslator nameTranslator)
    {
        (PgTypeName, NameTranslator) = (pgTypeName, nameTranslator);
        var fields = typeof(TEnum).GetFields(BindingFlags.Static | BindingFlags.Public);
        _enumToLabel = new(fields.Length);
        _labelToEnum = new(fields.Length);
        
        foreach (var field in fields)
        {
            var attribute = (PgNameAttribute?)field.GetCustomAttribute(typeof(PgNameAttribute), false);
            var enumName = attribute is null
                ? nameTranslator.TranslateMemberName(field.Name)
                : attribute.PgName;
            var enumValue = (TEnum)field.GetValue(null)!;

            _enumToLabel[enumValue] = enumName;
            _labelToEnum[enumName] = enumValue;
        }
    }

    public NpgsqlTypeHandler CreateHandler(PostgresType postgresType, NpgsqlConnector connector)
        => new EnumHandler<TEnum>((PostgresEnumType)postgresType, _enumToLabel, _labelToEnum);
}