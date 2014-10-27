using System;

namespace Bluewire.NHibernate.Audit.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class AuditableRelationAttribute : Attribute
    {
        public AuditableRelationAttribute(Type auditEntryType, string ownerKeyPropertyName, string keyPropertyName)
        {
            AuditEntryType = auditEntryType;
            OwnerKeyPropertyName = ownerKeyPropertyName;
            KeyPropertyName = keyPropertyName;
        }

        public Type AuditEntryType { get; private set; }
        public string OwnerKeyPropertyName { get; private set; }
        public string KeyPropertyName { get; private set; }
    }
}