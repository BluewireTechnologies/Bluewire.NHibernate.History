using System;
using System.Linq;
using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit
{
    public static class AuditableEntityExtensions
    {
        public static AuditableEntityAttribute GetAuditAttribute(this Type type)
        {
            return type.GetCustomAttributes(typeof(AuditableEntityAttribute), true).Cast<AuditableEntityAttribute>().SingleOrDefault();
        }

        public static bool IsAuditable(this Type type)
        {
            return type.GetAuditAttribute() != null;
        }
    }
}