using System;
using System.Data;
using System.Data.Common;
using NHibernate;
using NHibernate.Engine;
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

        public object NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner)
        {
            var ticks = NHibernateUtil.Int64.NullSafeGet(rs, names, session, owner);
            if (ticks == null) return null;
            return new DateTimeOffset((long)ticks, TimeSpan.Zero);
        }

        public void NullSafeSet(DbCommand cmd, object value, int index, ISessionImplementor session)
        {
            if (value == null)
            {
                NHibernateUtil.Int64.NullSafeSet(cmd, null, index, session);
            }
            else
            {
                var dateTimeOffset = (DateTimeOffset)value;
                NHibernateUtil.Int64.NullSafeSet(cmd, dateTimeOffset.UtcTicks, index, session);
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
