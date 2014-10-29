using NHibernate.Event;
using NHibernate.Persister.Collection;

namespace Bluewire.NHibernate.Audit.Listeners
{
    interface ICollectionAuditDeleteTask
    {
        ICollectionPersister Persister { get; }
        void Delete(object entry, int index);
        void Delete(object key);
        void DeleteAll();
        void Execute(IEventSource session);
    }
}
