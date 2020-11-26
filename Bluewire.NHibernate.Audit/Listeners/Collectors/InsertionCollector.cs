using System;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Persister.Collection;

namespace Bluewire.NHibernate.Audit.Listeners.Collectors
{
    public abstract class InsertionCollector : IChangeReceiver
    {
        protected InsertionCollector(CollectionEntry collectionEntry)
        {
            OwnerKey = collectionEntry.CurrentKey;
            Persister = collectionEntry.CurrentPersister;
            if (Persister == null) throw new ArgumentException("No CurrentPersister for collection.", "collectionEntry");
        }

        public object OwnerKey { get; private set; }
        public virtual void Prepare(IPersistentCollection collection) { }

        public ICollectionPersister Persister { get; private set; }

        public abstract bool IsEmpty { get; }

        public abstract void Insert(IPersistentCollection collection, object entry, int index);
        public abstract void Apply(IEventSource session, ValueCollectionAuditTasks task);

        public void Delete(IPersistentCollection collection, object entry, int index)
        {
        }

        public void Delete(IPersistentCollection collection, object key)
        {
        }

        public void Update(IPersistentCollection collection, object entry, int index)
        {
            Insert(collection, entry, index);
        }
    }
}
