using System;

namespace Bluewire.NHibernate.Audit.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class AuditableRelationAttribute : Attribute
    {
        public AuditableRelationAttribute(Type auditEntryType)
        {
            AuditEntryType = auditEntryType;
        }

        public Type AuditEntryType { get; private set; }
        /// <summary>
        /// Explicitly specify the collection element type which is recorded.
        /// Only valid for value types. Not currently used.
        /// </summary>
        public Type AuditValueType { get; set; }
    }
}
