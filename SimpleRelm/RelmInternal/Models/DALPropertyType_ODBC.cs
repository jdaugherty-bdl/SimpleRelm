using SimpleRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.RelmInternal.Models
{
    internal class DALPropertyType_ODBC
    {
        public string ColumnName { get; set; }
        public string PropertyName { get; set; }
        public PropertyInfo PropertyTypeInformation { get; set; }

        public string PropertyColumnType { get; private set; }
        public OdbcType PropertyOdbcDbType { get; private set; }
        public Type PropertyType { get; private set; }
        public int DefaultColumnSize { get; private set; }

        public RelmColumn ResolvableSettings { get; set; }

        // items first in list take precedence when converting from one tuple item to another - this list may look like it has duplicates, but it doesn't
        private static readonly IEnumerable<Tuple<string, OdbcType, Type, int>> MySqlTypeConverter = new List<Tuple<string, OdbcType, Type, int>>
        {
            new Tuple<string, OdbcType, Type, int>("bigint", OdbcType.BigInt, typeof(long), 20),
            new Tuple<string, OdbcType, Type, int>("bigint unsigned", OdbcType.BigInt, typeof(ulong), 20),
            new Tuple<string, OdbcType, Type, int>("varchar", OdbcType.VarChar, typeof(string), 45),
            new Tuple<string, OdbcType, Type, int>("char", OdbcType.VarChar, typeof(string), 45),
            new Tuple<string, OdbcType, Type, int>("smallint", OdbcType.SmallInt, typeof(short), -1),
            new Tuple<string, OdbcType, Type, int>("smallint unsigned", OdbcType.SmallInt, typeof(ushort), -1),
            new Tuple<string, OdbcType, Type, int>("int", OdbcType.Int, typeof(int), -1),
            new Tuple<string, OdbcType, Type, int>("int unsigned", OdbcType.Int, typeof(uint), -1),
            new Tuple<string, OdbcType, Type, int>("mediumint", OdbcType.Int, typeof(int), -1),
            new Tuple<string, OdbcType, Type, int>("mediumint unsigned", OdbcType.Int, typeof(uint), -1),
            new Tuple<string, OdbcType, Type, int>("tinyint", OdbcType.TinyInt, typeof(short), 1),
            new Tuple<string, OdbcType, Type, int>("tinyint unsigned", OdbcType.TinyInt, typeof(ushort), 1),
            new Tuple<string, OdbcType, Type, int>("tinyint", OdbcType.TinyInt, typeof(bool), 1),
            new Tuple<string, OdbcType, Type, int>("tinyint", OdbcType.TinyInt, typeof(byte), 1),
            new Tuple<string, OdbcType, Type, int>("bit", OdbcType.Bit, typeof(byte), -1),
            new Tuple<string, OdbcType, Type, int>("datetime", OdbcType.DateTime, typeof(DateTime), -1),
            new Tuple<string, OdbcType, Type, int>("timestamp", OdbcType.Timestamp, typeof(DateTime), -1),
            new Tuple<string, OdbcType, Type, int>("blob", OdbcType.Binary, typeof(string), -1),
            new Tuple<string, OdbcType, Type, int>("longblob", OdbcType.Binary, typeof(string), -1),
            new Tuple<string, OdbcType, Type, int>("mediumblob", OdbcType.Binary, typeof(string), -1),
            new Tuple<string, OdbcType, Type, int>("tinyblob", OdbcType.Binary, typeof(string), -1),
            new Tuple<string, OdbcType, Type, int>("binary", OdbcType.Binary, typeof(string), -1),
            new Tuple<string, OdbcType, Type, int>("varbinary", OdbcType.VarBinary, typeof(string), -1),
            new Tuple<string, OdbcType, Type, int>("varbinary", OdbcType.VarBinary, typeof(byte[]), -1),
            new Tuple<string, OdbcType, Type, int>("decimal", OdbcType.Decimal, typeof(decimal), -1),
            new Tuple<string, OdbcType, Type, int>("double", OdbcType.Double, typeof(double), -1),
            new Tuple<string, OdbcType, Type, int>("float", OdbcType.Real, typeof(float), -1),
            new Tuple<string, OdbcType, Type, int>("guid", OdbcType.VarChar, typeof(Guid),45),
            new Tuple<string, OdbcType, Type, int>("text", OdbcType.Text, typeof(string), -1),
            new Tuple<string, OdbcType, Type, int>("longtext", OdbcType.Text, typeof(string), -1),
            new Tuple<string, OdbcType, Type, int>("time", OdbcType.Time, typeof(DateTime), -1),
            new Tuple<string, OdbcType, Type, int>("date", OdbcType.Date, typeof(DateTime), -1),
            new Tuple<string, OdbcType, Type, int>("varchar", OdbcType.VarChar, typeof(object), 45),
            new Tuple<string, OdbcType, Type, int>("json", OdbcType.VarChar, typeof(object), -1),
            new Tuple<string, OdbcType, Type, int>("varchar", OdbcType.VarChar, typeof(TimeSpan), 45)
        };

        public static implicit operator string(DALPropertyType_ODBC Source) => MySqlTypeConverter.Where(x => x.Item1 == Source.PropertyColumnType).FirstOrDefault().Item1;
        public static implicit operator OdbcType(DALPropertyType_ODBC Source) => MySqlTypeConverter.Where(x => x.Item2 == Source.PropertyOdbcDbType).FirstOrDefault().Item2;
        public static implicit operator Type(DALPropertyType_ODBC Source) => MySqlTypeConverter.Where(x => x.Item3 == Source.PropertyType).FirstOrDefault().Item3;

        public static explicit operator DALPropertyType_ODBC(string Source) => new DALPropertyType_ODBC(Source);
        public static explicit operator DALPropertyType_ODBC(OdbcType Source) => new DALPropertyType_ODBC(Source);
        public static explicit operator DALPropertyType_ODBC(Type Source) => new DALPropertyType_ODBC(Source);

        public override string ToString() => $"[{PropertyColumnType} | {PropertyOdbcDbType} | {PropertyType}]";

        public override bool Equals(object SourcePropertyColumnName)
        {
            if (SourcePropertyColumnName.GetType() == typeof(string))
                return PropertyColumnType == (string)SourcePropertyColumnName;

            if (SourcePropertyColumnName.GetType() == typeof(OdbcType))
                return PropertyOdbcDbType == (OdbcType)SourcePropertyColumnName;

            if (SourcePropertyColumnName.GetType() == typeof(Type))
                return PropertyType == (Type)SourcePropertyColumnName;

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public DALPropertyType_ODBC(string SourcePropertyColumnName)
        {
            PropertyColumnType = SourcePropertyColumnName;
            PropertyOdbcDbType = ColumnNameToColumnType(PropertyColumnType);
            PropertyType = ColumnNameToPropertyType(PropertyColumnType);

            DefaultColumnSize = GetDefaultColumnSize(SourcePropertyColumnName);
        }

        public DALPropertyType_ODBC(OdbcType SourcePropertyColumnType)
        {
            PropertyOdbcDbType = SourcePropertyColumnType;
            PropertyColumnType = ColumnTypeToColumnName(PropertyOdbcDbType);
            PropertyType = ColumnTypeToPropertyType(PropertyOdbcDbType);

            DefaultColumnSize = GetDefaultColumnSize(SourcePropertyColumnType);
        }

        public DALPropertyType_ODBC(Type SourcePropertyType)
        {
            PropertyType = SourcePropertyType;

            if (SourcePropertyType.GetInterface(typeof(ICollection<>).Name) != null)
                PropertyType = typeof(string);

            PropertyColumnType = PropertyTypeToColumnName(PropertyType);
            PropertyOdbcDbType = PropertyTypeToColumnType(PropertyType);

            DefaultColumnSize = GetDefaultColumnSize(SourcePropertyType);
        }

        public static OdbcType ColumnNameToColumnType(string PropertyColumnName) => MySqlTypeConverter.Where(x => x.Item1 == PropertyColumnName).FirstOrDefault()?.Item2 ?? default;
        public static OdbcType PropertyTypeToColumnType(Type PropertyType) => MySqlTypeConverter.Where(x => x.Item3 == PropertyType).FirstOrDefault()?.Item2 ?? default;
        public static string ColumnTypeToColumnName(OdbcType PropertyColumnType) => MySqlTypeConverter.Where(x => x.Item2 == PropertyColumnType).FirstOrDefault()?.Item1;
        public static string PropertyTypeToColumnName(Type PropertyType) => MySqlTypeConverter.Where(x => x.Item3 == PropertyType).FirstOrDefault()?.Item1;
        public static Type ColumnNameToPropertyType(string PropertyColumnName) => MySqlTypeConverter.Where(x => x.Item1 == PropertyColumnName).FirstOrDefault()?.Item3;
        public static Type ColumnTypeToPropertyType(OdbcType PropertyColumnType) => MySqlTypeConverter.Where(x => x.Item2 == PropertyColumnType).FirstOrDefault()?.Item3;

        public static int GetDefaultColumnSize(string PropertyColumnName) => MySqlTypeConverter.Where(x => x.Item1 == PropertyColumnName).FirstOrDefault()?.Item4 ?? default;
        public static int GetDefaultColumnSize(OdbcType PropertyColumnType) => MySqlTypeConverter.Where(x => x.Item2 == PropertyColumnType).FirstOrDefault()?.Item4 ?? default;
        public static int GetDefaultColumnSize(Type PropertyType) => MySqlTypeConverter.Where(x => x.Item3 == PropertyType).FirstOrDefault()?.Item4 ?? default;
    }
}
