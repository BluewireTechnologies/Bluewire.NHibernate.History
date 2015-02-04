using Bluewire.NHibernate.Audit.Listeners.Collectors;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Event;

namespace Bluewire.NHibernate.Audit.Listeners
{
    /// <summary>
    /// Receives notifications of changes to an NHibernate collection.
    /// Used in conjunction with AuditCollectionListener.
    /// </summary>
    public abstract class CollectionChangeListener
    {
        /// <summary>
        /// All entries in the collection were deleted and recreated in another collection.
        /// </summary>
        /// <param name="collectionEntry"></param>
        /// <param name="collection"></param>
        /// <param name="session"></param>
        public virtual void CollectionWasMoved(CollectionEntry collectionEntry, IPersistentCollection collection, IEventSource session)
        {
            CollectionWasDestroyed(collectionEntry, collection, session);
            CollectionWasCreated(collectionEntry, collection, session);
        }
        /// <summary>
        /// All entries in the collection were deleted and not recreated elsewhere.
        /// </summary>
        /// <param name="collectionEntry"></param>
        /// <param name="collection"></param>
        /// <param name="session"></param>
        public abstract void CollectionWasDestroyed(CollectionEntry collectionEntry, IPersistentCollection collection, IEventSource session);
        /// <summary>
        /// The collection was populated from empty.
        /// </summary>
        /// <param name="collectionEntry"></param>
        /// <param name="collection"></param>
        /// <param name="session"></param>
        public abstract void CollectionWasCreated(CollectionEntry collectionEntry, IPersistentCollection collection, IEventSource session);
        /// <summary>
        /// One or more entries in the collection were removed, added or modified.
        /// </summary>
        /// <param name="collectionEntry"></param>
        /// <param name="collection"></param>
        /// <param name="session"></param>
        public abstract void CollectionWasModified(CollectionEntry collectionEntry, IPersistentCollection collection, IEventSource session);
    }
}