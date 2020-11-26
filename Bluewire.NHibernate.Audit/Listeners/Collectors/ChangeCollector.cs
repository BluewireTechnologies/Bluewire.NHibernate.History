using System;
using System.Diagnostics;
using System.Linq;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Persister.Collection;

namespace Bluewire.NHibernate.Audit.Listeners.Collectors
{
    public class ChangeCollector
    {
        private readonly CollectionEntry collectionEntry;
        private readonly IPersistentCollection collection;

        private readonly ICollectionPersister deletePersister;
        private readonly ICollectionPersister insertPersister;

        public ChangeCollector(CollectionEntry collectionEntry, IPersistentCollection collection)
        {
            deletePersister = collectionEntry.LoadedPersister;
            insertPersister = collectionEntry.CurrentPersister;

            this.collectionEntry = collectionEntry;
            this.collection = collection;
        }

        /// <summary>
        /// Record deletion of all items against a receiver.
        /// </summary>
        /// <remarks>
        /// Throws if the receiver's Persister does not match the deletion persister.
        /// </remarks>
        /// <param name="receiver"></param>
        public void DeleteAll(IChangeReceiver receiver)
        {
            if (receiver.Persister != deletePersister) throw new InvalidOperationException();
            if (deletePersister == null) throw new ArgumentException("No LoadedPersister for collection.", "collectionEntry");

            var emptySnapshot = collectionEntry.IsSnapshotEmpty(collection);
            if (emptySnapshot) return;

            receiver.Prepare(collection);
            var index = 0;
            foreach (var item in collection.Entries(deletePersister))
            {
                receiver.Delete(collection, item, index);
                index++;
            }
        }

        /// <summary>
        /// Record insertion of all items against a receiver.
        /// </summary>
        /// <remarks>
        /// Throws if the receiver's Persister does not match the insertion persister.
        /// </remarks>
        /// <param name="receiver"></param>
        public void InsertAll(IChangeReceiver receiver)
        {
            if (receiver.Persister != insertPersister) throw new InvalidOperationException();

            receiver.Prepare(collection);
            var index = 0;
            foreach (var item in collection.Entries(insertPersister))
            {
                receiver.Insert(collection, item, index);
                ++index;
            }
        }
        /// <summary>
        /// Record inserts, updates and deletes against a receiver.
        /// </summary>
        /// <remarks>
        /// Records only the changes relevant to specified receiver.
        /// </remarks>
        /// <param name="receiver"></param>
        public void Collect(IChangeReceiver receiver)
        {
            var receivesDeletes = receiver.Persister == deletePersister;
            var receivesInserts = receiver.Persister == insertPersister;
            receiver.Prepare(collection);
            if (receivesDeletes)
            {
                var deleted = collection.GetDeletes(deletePersister, false).Cast<object>();
                foreach (var d in deleted)
                {
                    receiver.Delete(collection, d);
                }
                if (!receivesInserts)
                {
                    EnumerateDeletes(receiver);
                    return;
                }
            }
            if (receivesInserts)
            {
                EnumerateUpdatesInserts(receiver);
            }
        }

        private void EnumerateDeletes(IChangeReceiver receiver)
        {
            Debug.Assert(receiver.Persister == deletePersister);
            var index = 0;
            foreach (var entry in collection.Entries(deletePersister))
            {
                if (collection.NeedsUpdating(entry, index, deletePersister.ElementType))
                {
                    receiver.Update(collection, entry, index);
                }
                ++index;
            }
        }

        private void EnumerateUpdatesInserts(IChangeReceiver receiver)
        {
            Debug.Assert(receiver.Persister == insertPersister);
            var index = 0;
            foreach (var entry in collection.Entries(insertPersister))
            {
                if (collection.NeedsUpdating(entry, index, insertPersister.ElementType))
                {
                    receiver.Update(collection, entry, index);
                }
                else if (collection.NeedsInserting(entry, index, insertPersister.ElementType))
                {
                    receiver.Insert(collection, entry, index);
                }
                ++index;
            }
        }
    }
}
