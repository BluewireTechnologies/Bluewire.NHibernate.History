using Bluewire.NHibernate.Audit.Model;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Event;

namespace Bluewire.NHibernate.Audit.Listeners
{
    public class AuditCollectionElementInsertionListener : AuditCollectionListenerBase
    {
        private readonly SessionsAuditInfo sessions;
        private readonly AuditModel model;

        public AuditCollectionElementInsertionListener(SessionsAuditInfo sessions, AuditModel model)
        {
            this.sessions = sessions;
            this.model = model;
        }
        
        protected override void CollectionWasDestroyed(CollectionEntry collectionEntry, IPersistentCollection collection, IEventSource session)
        {
        }

        protected override void CollectionWasCreated(CollectionEntry collectionEntry, IPersistentCollection collection, IEventSource session)
        {
            if (!model.IsAuditable(collectionEntry.CurrentPersister)) return;

            var task = GetInsertTask(collectionEntry, collection, session);
            task.InsertAll();
            task.Execute(session);
        }

        protected override void CollectionWasModified(CollectionEntry collectionEntry, IPersistentCollection collection, IEventSource session)
        {
            if (!model.IsAuditable(collectionEntry.CurrentPersister)) return;

            var task = GetInsertTask(collectionEntry, collection, session);
            var index = 0;
            foreach (var entry in collection.Entries(task.Persister))
            {
                if (collection.NeedsUpdating(entry, index, task.Persister.ElementType) ||
                    collection.NeedsInserting(entry, index, task.Persister.ElementType))
                {
                    task.Insert(entry, index);
                }
                ++index;
            }
            task.Execute(session);
        }

        private ICollectionAuditInsertTask GetInsertTask(CollectionEntry collectionEntry, IPersistentCollection collection, IEventSource session)
        {
            if (collectionEntry.CurrentPersister.HasIndex)
            {
                return new KeyedCollectionAuditInsertTask(collectionEntry, collection, sessions.Lookup(session), model);
            }
            return new SetCollectionAuditInsertTask(collectionEntry, collection, sessions.Lookup(session), model);
        }
    }
}