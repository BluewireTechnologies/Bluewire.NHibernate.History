using System;
using NHibernate;
using NHibernate.Engine;

namespace Bluewire.NHibernate.Audit.Runtime
{
    public interface IDeferredAudit
    {
        void QueueInsert(object item);
        void QueueWork(Action<ISessionImplementor, ISessionSnapshot> work);
        void QueueWork(IWorkItem work);
        void QueueFixup(Action<ISessionImplementor, SessionAuditInfo> fixup);
        void QueueFixup(IFixup fixup);
    }
}