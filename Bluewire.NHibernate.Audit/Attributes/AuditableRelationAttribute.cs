using System;

namespace Bluewire.NHibernate.Audit.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class AuditableRelationAttribute : Attribute
    {
        public AuditableRelationAttribute(Type auditEntryType, string ownerKeyPropertyName)
        {
            AuditEntryType = auditEntryType;
            OwnerKeyPropertyName = ownerKeyPropertyName;
        }

        public Type AuditEntryType { get; private set; }
        public string OwnerKeyPropertyName { get; private set; }
    }
}