using System;

namespace Bluewire.NHibernate.Audit.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class AuditableEntityAttribute : Attribute
    {
        public AuditableEntityAttribute(Type auditEntryType)
        {
            AuditEntryType = auditEntryType;
        }

        public Type AuditEntryType { get; private set; }
    }
}
