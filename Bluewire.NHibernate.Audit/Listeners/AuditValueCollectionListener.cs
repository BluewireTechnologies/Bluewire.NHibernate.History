using System.Linq;
using Bluewire.NHibernate.Audit.Listeners.Collectors;
using Bluewire.NHibernate.Audit.Model;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Event;

namespace Bluewire.NHibernate.Audit.Listeners
{
    public class AuditValueCollectionListener : CollectionChangeListener
    {
        private readonly AuditModel model;
        private readonly ValueCollectionAuditTasks auditTask;

        public AuditValueCollectionListener(SessionsAuditInfo sessions, AuditModel model)
        {
            this.model = model;
            this.auditTask = new ValueCollectionAuditTasks(sessions, model);
        }
        
        public override void CollectionWasDestroyed(CollectionEntry collectionEntry, IPersistentCollection collection, IEventSource session)
        {
            if (!model.IsAuditable(collectionEntry.LoadedPersister)) return;

            var deletionCollector = new DeletionCollector(collectionEntry);
            var collector = new ChangeCollector(collectionEntry, collection);

            collector.DeleteAll(deletionCollector);

            deletionCollector.Apply(session, auditTask);
        }

        public override void CollectionWasCreated(CollectionEntry collectionEntry, IPersistentCollection collection, IEventSource session)
        {
            if (!model.IsAuditable(collectionEntry.CurrentPersister)) return;

            var insertionCollector = GetInsertCollector(collectionEntry);
            var collector = new ChangeCollector(collectionEntry, collection);

            collector.InsertAll(insertionCollector);

            insertionCollector.Apply(session, auditTask);
        }

        public override void CollectionWasModified(CollectionEntry collectionEntry, IPersistentCollection collection, IEventSource session)
        {
            var collector = new ChangeCollector(collectionEntry, collection);

            // Deletion, then insertion.
            if (model.IsAuditable(collectionEntry.LoadedPersister))
            {
                var deletionCollector = new DeletionCollector(collectionEntry);
                collector.Collect(deletionCollector);

                deletionCollector.Apply(session, auditTask);
            }
            if (model.IsAuditable(collectionEntry.CurrentPersister))
            {
                var insertionCollector = GetInsertCollector(collectionEntry);
                collector.Collect(insertionCollector);
                insertionCollector.Apply(session, auditTask);
            }
        }

        private static InsertionCollector GetInsertCollector(CollectionEntry collectionEntry)
        {
            if (collectionEntry.CurrentPersister.HasIndex)
            {
                return new KeyedInsertionCollector(collectionEntry);
            }
            return new SetInsertionCollector(collectionEntry);
        }
    }
}