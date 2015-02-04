using System;
using System.Diagnostics;
using NHibernate.Engine;

namespace Bluewire.NHibernate.Audit.Model
{
    /// <summary>
    /// The identity of a value type is its value, which may be mapped to an alternative type for auditing.
    /// </summary>
    class ComponentCollectionAuditValueResolver : IRelationAuditValueResolver
    {
        public object Resolve(object collectionElement, ISessionImplementor session, Type sourceType, Type auditType, IAuditEntryFactory auditEntryFactory)
        {
            if (sourceType == auditType) return collectionElement;

            var auditValue = auditEntryFactory.CreateComponent(collectionElement, sourceType, auditType);
            Debug.Assert(auditType.IsInstanceOfType(auditValue));
            return auditValue;
        }
    }
}