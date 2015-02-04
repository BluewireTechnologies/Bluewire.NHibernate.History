using System;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Persister.Collection;

namespace Bluewire.NHibernate.Audit.Listeners.Collectors
{
    public abstract class InsertionCollector
    {
        protected IPersistentCollection Collection {get; private set;}

        protected InsertionCollector(CollectionEntry collectionEntry, IPersistentCollection collection)
        {
            Key = collectionEntry.CurrentKey;
            Persister = collectionEntry.CurrentPersister;
            if (Persister == null) throw new ArgumentException("No CurrentPersister for collection.", "collectionEntry");
            this.Collection = collection;
        }

        public object Key { get; private set; }
        public ICollectionPersister Persister { get; private set; }

        public void InsertAll()
        {
            var index = 0;
            foreach (var item in Collection.Entries(Persister))
            {
                Insert(item, index);
                ++index;
            }
        }

        public void CollectInsertions()
        {
            var index = 0;
            foreach (var entry in Collection.Entries(Persister))
            {
                if (Collection.NeedsUpdating(entry, index, Persister.ElementType) ||
                    Collection.NeedsInserting(entry, index, Persister.ElementType))
                {
                    Insert(entry, index);
                }
                ++index;
            }
        }
        public abstract bool IsEmpty { get; }

        protected abstract void Insert(object entry, int index);
        public abstract void Apply(IEventSource session, ValueCollectionAuditTasks task);
    }
}