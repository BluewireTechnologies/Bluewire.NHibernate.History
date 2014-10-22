using System;
using NHibernate;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;

namespace Bluewire.NHibernate.Audit.Support
{
    /// <summary>
    /// Allows for storing DateTimeOffsets semi-sanely in SQLite. Stores UTC tick count, ie. retains
    /// point-in-time but loses timezone information.
    /// </summary>
    public class DateTimeOffsetAsIntegerUserType : IUserType
    {
        public object Assemble(object cached, object owner)
        {
            return cached;
        }

        public object DeepCopy(object value)
        {
            return value;
        }

        public object Disassemble(object value)
        {
            return value;
        }

        public new bool Equals(object x, object y)
        {
            return Object.Equals(x, y);
        }

        public int GetHashCode(object x)
        {
            return x.GetHashCode();
        }

        public bool IsMutable
        {
            get { return false; }
        }

        public object NullSafeGet(System.Data.IDataReader rs, string[] names, object owner)
        {
            var ticks = NHibernateUtil.Int64.NullSafeGet(rs, names);
            if (ticks == null) return null;
            return new DateTimeOffset((long)ticks, TimeSpan.Zero);
        }

        public void NullSafeSet(System.Data.IDbCommand cmd, object value, int index)
        {
            if (value == null)
            {
                NHibernateUtil.Int64.NullSafeSet(cmd, null, index);
            }
            else
            {
                var dateTimeOffset = (DateTimeOffset)value;
                NHibernateUtil.Int64.NullSafeSet(cmd, dateTimeOffset.UtcTicks, index);
            }
        }

        public object Replace(object original, object target, object owner)
        {
            return original;
        }

        public Type ReturnedType
        {
            get { return typeof(DateTimeOffset); }
        }

        public SqlType[] SqlTypes
        {
            get { return new[] { NHibernateUtil.Int64.SqlType }; }
        }
    }
}
