using System;
using NHibernate.Engine;

namespace Bluewire.NHibernate.Audit.Model
{
    public interface IRelationAuditValueResolver
    {
        object Resolve(object collectionElement, ISessionImplementor session, Type sourceType, Type auditType, IAuditEntryFactory auditEntryFactory);
    }
}