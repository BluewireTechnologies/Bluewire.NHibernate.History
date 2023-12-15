using NHibernate;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Event;

namespace Bluewire.NHibernate.Audit.Listeners
{
    /// <summary>
    /// Maps NHibernate's collection-related events onto a more audit-friendly model.
    /// </summary>
    /// <remarks>
    /// For most audit purposes, details about elements being copied or moved between collections are not relevant.
    /// The only things we care about are:
    ///  * Initial creation of a collection (insert all elements)
    ///  * Deletion of a collection (mark all elements as expired)
    ///  * Modifications to a collection (insert and/or expire on a per-element basis)
    ///
    /// This class encapsulates my understanding of NHibernate's collection lifecycles.
    /// Most of it was reverse-engineered from CollectionUpdateAction and AbstractCollectionPersister.
    /// Note that the concept of 'UpdateRows' is not relevant to value-type collections; that's
    /// handled as a delete and insert. Listeners which do care about this distinction need to correlate
    /// entries to reverse-engineer what was done to the collection.
    /// </remarks>
    public class AuditCollectionListener : IPreCollectionRecreateEventListener, IPreCollectionUpdateEventListener, IPreCollectionRemoveEventListener
    {
        private readonly CollectionChangeListener[] children;

        public AuditCollectionListener(params CollectionChangeListener[] children)
        {
            this.children = children;
        }

        public void OnPreUpdateCollection(PreCollectionUpdateEvent @event)
        {
            var collection = @event.Collection;
            var collectionEntry = @event.Session.PersistenceContext.GetCollectionEntry(@event.Collection);

            var hasFilters = collectionEntry.LoadedPersister.IsAffectedByEnabledFilters(@event.Session);
            if (!collection.WasInitialized)
            {
                if (!collection.HasQueuedOperations)
                {
                    throw new AssertionFailure("no queued adds");
                }
            }
            else if (!hasFilters && collection.Empty)
            {
                CollectionWasDestroyed(collectionEntry, collection, @event.Session);
            }
            else if (collection.NeedsRecreate(collectionEntry.CurrentPersister))
            {
                if (hasFilters) throw new HibernateException("cannot recreate collection while filter is enabled");
                CollectionWasMoved(collectionEntry, @event.Collection, @event.Session);
            }
            else
            {
                CollectionWasModified(collectionEntry, collection, @event.Session);
            }
        }

        public void OnPreRemoveCollection(PreCollectionRemoveEvent @event)
        {
            var collectionEntry = @event.Session.PersistenceContext.GetCollectionEntry(@event.Collection);
            if (collectionEntry.LoadedPersister == null) return;

            CollectionWasDestroyed(collectionEntry, @event.Collection, @event.Session);
        }

        public void OnPreRecreateCollection(PreCollectionRecreateEvent @event)
        {
            var collectionEntry = @event.Session.PersistenceContext.GetCollectionEntry(@event.Collection);
            if (collectionEntry.LoadedPersister != null)
            {
                CollectionWasMoved(collectionEntry, @event.Collection, @event.Session);
            }
            else
            {
                CollectionWasCreated(collectionEntry, @event.Collection, @event.Session);
            }
        }


        /// <summary>
        /// All entries in the collection were deleted and recreated in another collection.
        /// </summary>
        protected virtual void CollectionWasMoved(CollectionEntry collectionEntry, IPersistentCollection collection, IEventSource session)
        {
            foreach (var c in children) c.CollectionWasMoved(collectionEntry, collection, session);
        }
        /// <summary>
        /// All entries in the collection were deleted and not recreated elsewhere.
        /// </summary>
        private void CollectionWasDestroyed(CollectionEntry collectionEntry, IPersistentCollection collection, IEventSource session)
        {
            foreach (var c in children) c.CollectionWasDestroyed(collectionEntry, collection, session);
        }
        /// <summary>
        /// The collection was populated from empty.
        /// </summary>
        private void CollectionWasCreated(CollectionEntry collectionEntry, IPersistentCollection collection, IEventSource session)
        {
            foreach (var c in children) c.CollectionWasCreated(collectionEntry, collection, session);
        }
        /// <summary>
        /// One or more entries in the collection were removed, added or modified.
        /// </summary>
        private void CollectionWasModified(CollectionEntry collectionEntry, IPersistentCollection collection, IEventSource session)
        {
            foreach (var c in children) c.CollectionWasModified(collectionEntry, collection, session);
        }
    }
}
