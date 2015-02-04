using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Persister.Collection;

namespace Bluewire.NHibernate.Audit.Listeners.Collectors
{
    public class DeletionCollector
    {
        private readonly CollectionEntry collectionEntry;
        private readonly IPersistentCollection collection;

        public object OwnerKey { get; private set; }
        public ICollectionPersister Persister { get; private set; }

        public DeletionCollector(CollectionEntry collectionEntry, IPersistentCollection collection)
        {
            Persister = collectionEntry.LoadedPersister;
            OwnerKey = collectionEntry.LoadedKey;
            if (Persister == null) throw new ArgumentException("No LoadedPersister for collection.", "collectionEntry");

            this.collectionEntry = collectionEntry;
            this.collection = collection;
        }

        readonly List<object> deletions = new List<object>();

        public void DeleteAll()
        {
            var emptySnapshot = collectionEntry.IsSnapshotEmpty(collection);
            if (emptySnapshot) return;

            var index = 0;
            foreach (var item in collection.Entries(Persister))
            {
                Delete(item, index);
                index++;
            }
        }

        private void Delete(object entry, int index)
        {
            var key = collection.GetIndex(entry, index, Persister);
            deletions.Add(key);
        }

        private void Delete(object key)
        {
            deletions.Add(key);
        }

        public bool IsEmpty { get { return !deletions.Any(); } }

        public IEnumerable<object> Enumerate()
        {
            return deletions;
        }

        public void CollectDeletions()
        {
            var deleted = collection.GetDeletes(Persister, false).Cast<object>();
            foreach (var d in deleted)
            {
                Delete(d);
            }
            var index = 0;
            foreach (var entry in collection.Entries(Persister))
            {
                if (collection.NeedsUpdating(entry, index, Persister.ElementType))
                {
                    Delete(entry, index);
                }
                ++index;
            }
        }

        public void Apply(IEventSource session, ValueCollectionAuditTasks task)
        {
            if (Persister.HasIndex)
            {
                task.ExecuteKeyedDeletion(session, this);
            }
            else
            {
                task.ExecuteSetDeletion(session, this);
            }
        }
    }
}