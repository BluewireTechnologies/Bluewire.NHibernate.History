using System;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Persister.Collection;

namespace Bluewire.NHibernate.Audit.Listeners.Collectors
{
    public abstract class DeletionCollector : IChangeReceiver
    {
        protected DeletionCollector(CollectionEntry collectionEntry)
        {
            Persister = collectionEntry.LoadedPersister;
            OwnerKey = collectionEntry.LoadedKey;
            if (Persister == null) throw new ArgumentException("No LoadedPersister for collection.", "collectionEntry");

        }

        public object OwnerKey { get; private set; }

        public ICollectionPersister Persister { get; private set; }

        public abstract void Delete(IPersistentCollection collection, object entry, int index);
        public abstract void Delete(IPersistentCollection collection, object key);
        public abstract void Apply(IEventSource session, ValueCollectionAuditTasks task);
        
        public void Insert(IPersistentCollection collection, object entry, int index)
        {
        }

        public void Update(IPersistentCollection collection, object entry, int index)
        {
            Delete(collection, entry, index);
        }
    }
}