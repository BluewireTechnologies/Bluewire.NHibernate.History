using System.Linq;
using Bluewire.NHibernate.Audit.Model;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Event;

namespace Bluewire.NHibernate.Audit.Listeners
{
    public class AuditCollectionElementDeletionListener : AuditCollectionListenerBase
    {
        private readonly SessionsAuditInfo sessions;
        private readonly AuditModel model;

        public AuditCollectionElementDeletionListener(SessionsAuditInfo sessions, AuditModel model)
        {
            this.sessions = sessions;
            this.model = model;
        }

        protected override void CollectionWasDestroyed(CollectionEntry collectionEntry, IPersistentCollection collection, IEventSource session)
        {
            if (!model.IsAuditable(collectionEntry.LoadedPersister)) return;

            var task = GetDeleteTask(collectionEntry, collection, session);
            task.DeleteAll();
            task.Execute(session);
        }

        protected override void CollectionWasCreated(CollectionEntry collectionEntry, IPersistentCollection collection, IEventSource session)
        {
        }

        protected override void CollectionWasModified(CollectionEntry collectionEntry, IPersistentCollection collection, IEventSource session)
        {
            if (!model.IsAuditable(collectionEntry.LoadedPersister)) return;

            var task = GetDeleteTask(collectionEntry, collection, session);

            var deletions = collection.GetDeletes(task.Persister, false).Cast<object>();
            foreach (var d in deletions)
            {
                task.Delete(d);
            }
            var index = 0;
            foreach (var entry in collection.Entries(task.Persister))
            {
                if (collection.NeedsUpdating(entry, index, task.Persister.ElementType))
                {
                    task.Delete(entry, index);
                }
                ++index;
            }
            task.Execute(session);
        }

        private ICollectionAuditDeleteTask GetDeleteTask(CollectionEntry collectionEntry, IPersistentCollection collection, IEventSource session)
        {
            if (collectionEntry.LoadedPersister.HasIndex)
            {
                return new KeyedCollectionAuditDeleteTask(collectionEntry, collection, sessions.Lookup(session), model);
            }
            return new SetCollectionAuditDeleteTask(collectionEntry, collection, sessions.Lookup(session), model);
        }
    }
}