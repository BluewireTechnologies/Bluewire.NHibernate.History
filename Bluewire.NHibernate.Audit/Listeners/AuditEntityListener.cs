using System.Diagnostics;
using System.Linq;
using Bluewire.NHibernate.Audit.Model;
using Bluewire.NHibernate.Audit.Runtime;
using Iesi.Collections;
using NHibernate;
using NHibernate.Event;
using NHibernate.Persister.Entity;

namespace Bluewire.NHibernate.Audit.Listeners
{
    class AuditEntityListener : IFlushEntityEventListener, IDeleteEventListener
    {
        private readonly SessionsAuditInfo sessions;
        private readonly AuditModel model;

        public AuditEntityListener(SessionsAuditInfo sessions, AuditModel model)
        {
            this.sessions = sessions;
            this.model = model;
        }

        public void OnFlushEntity(FlushEntityEvent @event)
        {
            if (@event.EntityEntry.ExistsInDatabase && (@event.DirtyProperties == null || !@event.DirtyProperties.Any())) return;

            IAuditableEntityModel entityModel;
            if (!model.TryGetModelForPersister(@event.EntityEntry.Persister, out entityModel)) return;

            var sessionAuditInfo = sessions.Lookup(@event.Session);
            sessionAuditInfo.AssertIsFlushing();

            var auditEntry = model.GenerateAuditEntry(entityModel, @event.Entity);
            auditEntry.AuditDatestamp = sessionAuditInfo.OperationDatestamp;

            if (@event.EntityEntry.ExistsInDatabase)
            {
                AuditUpdate(auditEntry, @event);
            }
            else
            {
                AuditAdd(auditEntry, @event);
            }

            Debug.Assert(Equals(auditEntry.Id, @event.EntityEntry.EntityKey.Identifier));
            Debug.Assert(!Equals(auditEntry.VersionId, auditEntry.PreviousVersionId));

            sessionAuditInfo.CurrentModel.QueueInsert(auditEntry);
        }

        public void OnDelete(DeleteEvent @event, ISet transientEntities)
        {
            //OnDelete(@event);
        }

        public void OnDelete(DeleteEvent @event)
        {
            var persister = @event.Session.GetEntityPersister(@event.EntityName, @event.Entity);
            IAuditableEntityModel entityModel;
            if (!model.TryGetModelForPersister(persister, out entityModel)) return;

            var sessionAuditInfo = sessions.Lookup(@event.Session);

            var auditEntry = model.GenerateAuditEntry(entityModel, @event.Entity);
            auditEntry.AuditDatestamp = sessionAuditInfo.OperationDatestamp;

            AuditDelete(auditEntry, @event, persister);

            Debug.Assert(Equals(auditEntry.Id, persister.GetIdentifier(@event.Entity, EntityMode.Poco)));
            Debug.Assert(!Equals(auditEntry.VersionId, auditEntry.PreviousVersionId));

            sessionAuditInfo.CurrentModel.QueueInsert(auditEntry);
        }

        private static void AuditAdd(IEntityAuditHistory auditEntry, FlushEntityEvent @event)
        {
            auditEntry.AuditedOperation = AuditedOperation.Add;
            auditEntry.PreviousVersionId = null;
            auditEntry.VersionId = @event.EntityEntry.Persister.GetVersion(@event.Entity, EntityMode.Poco);
        }

        private static void AuditUpdate(IEntityAuditHistory auditEntry, FlushEntityEvent @event)
        {
            auditEntry.AuditedOperation = AuditedOperation.Update;
            auditEntry.PreviousVersionId = @event.EntityEntry.Version;
            auditEntry.VersionId = @event.PropertyValues[@event.EntityEntry.Persister.VersionProperty];
        }

        private static void AuditDelete(IEntityAuditHistory auditEntry, DeleteEvent @event, IEntityPersister persister)
        {
            auditEntry.AuditedOperation = AuditedOperation.Delete;
            auditEntry.PreviousVersionId = persister.GetVersion(@event.Entity, EntityMode.Poco);
            auditEntry.VersionId = null;
        }
    }
}