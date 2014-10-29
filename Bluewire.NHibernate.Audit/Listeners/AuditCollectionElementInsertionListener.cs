using Bluewire.NHibernate.Audit.Model;
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
        
        protected override void CollectionWasDestroyed(global::NHibernate.Engine.CollectionEntry collectionEntry, global::NHibernate.Collection.IPersistentCollection collection, IEventSource session)
        {
        }

        protected override void CollectionWasCreated(global::NHibernate.Engine.CollectionEntry collectionEntry, global::NHibernate.Collection.IPersistentCollection collection, IEventSource session)
        {
            var task = new KeyedCollectionAuditInsertTask(collectionEntry, collection, sessions.Lookup(session), model);
            task.InsertAll();
            task.Execute(session);
        }

        protected override void CollectionWasModified(global::NHibernate.Engine.CollectionEntry collectionEntry, global::NHibernate.Collection.IPersistentCollection collection, IEventSource session)
        {
            var task = new KeyedCollectionAuditInsertTask(collectionEntry, collection, sessions.Lookup(session), model);
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
    }
}