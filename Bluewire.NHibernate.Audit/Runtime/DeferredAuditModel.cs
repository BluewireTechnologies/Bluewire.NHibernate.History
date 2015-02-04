using System;
using System.Collections.Generic;
using NHibernate;
using NHibernate.Engine;

namespace Bluewire.NHibernate.Audit.Runtime
{
    /// <summary>
    /// Enables queueing of audit-related actions to execute prior to completion of flush.
    /// </summary>
    /// <remarks>
    /// Some audit operations may depend on others having already been prepared. One-to-many relationships between entity
    /// types is one example of this, where the collection audit step needs to modify the entity audit entry prior to saving.
    /// NHibernate is inflexible in  its own processing order for various good reasons, so we need to build a model of what
    /// we plan to do and apply it later.
    /// </remarks>
    public class DeferredAuditModel : IDeferredAudit
    {
        public DeferredAuditModel()
        {
            PendingFixups = new Queue<IFixup>();
            PendingWorkItems = new Queue<IWorkItem>();
            PendingInserts = new Queue<object>();
        }

        public Queue<IFixup> PendingFixups { get; private set; }
        public Queue<IWorkItem> PendingWorkItems { get; private set; }
        public Queue<object> PendingInserts { get; private set; }

        public void QueueInsert(object item)
        {
            PendingInserts.Enqueue(item);
        }

        public void QueueWork(Action<ISessionImplementor, ISessionSnapshot> work)
        {
            PendingWorkItems.Enqueue(new DelegateWorkItem(work));
        }

        public void QueueWork(IWorkItem work)
        {
            PendingWorkItems.Enqueue(work);
        }

        public void QueueFixup(Action<ISessionImplementor, SessionAuditInfo> fixup)
        {
            PendingFixups.Enqueue(new DelegateFixup(fixup));
        }

        public void QueueFixup(IFixup fixup)
        {
            PendingFixups.Enqueue(fixup);
        }

        class DelegateFixup : IFixup
        {
            private readonly Action<ISessionImplementor, SessionAuditInfo> fixup;

            public DelegateFixup(Action<ISessionImplementor, SessionAuditInfo> fixup)
            {
                this.fixup = fixup;
            }

            public void Apply(ISessionImplementor session, SessionAuditInfo sessionAuditInfo)
            {
                fixup(session, sessionAuditInfo);
            }
        }

        class DelegateWorkItem : IWorkItem
        {
            private readonly Action<ISessionImplementor, ISessionSnapshot> work;

            public DelegateWorkItem(Action<ISessionImplementor, ISessionSnapshot> work)
            {
                this.work = work;
            }

            public void Execute(ISessionImplementor session, ISessionSnapshot snapshot)
            {
                work(session, snapshot);
            }
        }
    }
}