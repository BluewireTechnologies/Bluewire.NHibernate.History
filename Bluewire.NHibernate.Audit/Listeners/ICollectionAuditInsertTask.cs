using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Persister.Collection;

namespace Bluewire.NHibernate.Audit.Listeners
{
    interface ICollectionAuditInsertTask
    {
        ICollectionPersister Persister { get; }
        void Insert(object entry, int index);
        void InsertAll();
        void Execute(IEventSource session);
    }
}