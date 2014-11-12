using System;

namespace Bluewire.NHibernate.Audit.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class AuditableRelationAttribute : Attribute
    {
        public AuditableRelationAttribute(Type auditEntryType, Type auditValueType = null)
        {
            AuditEntryType = auditEntryType;
            AuditValueType = auditValueType;
        }

        public Type AuditEntryType { get; private set; }
        public Type AuditValueType { get; private set; }
    }
}