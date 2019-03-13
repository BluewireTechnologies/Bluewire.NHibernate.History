using System;
using NHibernate.Engine;
using NHibernate.Type;

namespace Bluewire.NHibernate.Audit.Model
{
    /// <summary>
    /// The identity of a reference type is its ID.
    /// </summary>
    class ReferenceRelationAuditValueResolver : IRelationAuditValueResolver
    {
        private readonly ManyToOneType type;

        public ReferenceRelationAuditValueResolver(ManyToOneType type)
        {
            this.type = type;
        }

        public object Resolve(object collectionElement, ISessionImplementor session, Type sourceType, Type auditType, IAuditEntryFactory auditEntryFactory)
        {
            return ForeignKeys.GetEntityIdentifierIfNotUnsaved(type.GetAssociatedEntityName(), collectionElement, session);
        }
    }
}
