using Bluewire.NHibernate.Audit.Listeners.Collectors;
using Bluewire.NHibernate.Audit.Model;
using Bluewire.NHibernate.Audit.Runtime;
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

            var deletionCollector = GetDeleteCollector(collectionEntry);
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
                var deletionCollector = GetDeleteCollector(collectionEntry);
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
            if (collectionEntry.CurrentPersister.IdentifierGenerator != null)
            {
                return new IdentifiedInsertionCollector(collectionEntry);
            }
            return new SetInsertionCollector(collectionEntry);
        }

        private static DeletionCollector GetDeleteCollector(CollectionEntry collectionEntry)
        {
            if (collectionEntry.LoadedPersister.HasIndex)
            {
                return new KeyedDeletionCollector(collectionEntry);
            }
            if (collectionEntry.LoadedPersister.IdentifierGenerator != null)
            {
                return new IdentifiedDeletionCollector(collectionEntry);
            }
            return new SetDeletionCollector(collectionEntry);
        }
    }
}
