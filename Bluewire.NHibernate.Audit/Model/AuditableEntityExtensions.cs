using System;
using System.Linq;
using System.Reflection;
using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.Model
{
    public static class AuditableEntityExtensions
    {
        public static AuditableEntityAttribute GetAuditAttribute(this Type type)
        {
            return type.GetCustomAttributes(typeof(AuditableEntityAttribute), true).Cast<AuditableEntityAttribute>().SingleOrDefault();
        }

        public static AuditableRelationAttribute GetAuditRelationAttribute(this PropertyInfo prop)
        {
            return prop.GetCustomAttributes(typeof(AuditableRelationAttribute), true).Cast<AuditableRelationAttribute>().SingleOrDefault();
        }

        public static bool IsAuditable(this Type type)
        {
            return type.GetAuditAttribute() != null;
        }
    }
}