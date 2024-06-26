﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bluewire.NHibernate.Audit.Meta;
using Bluewire.NHibernate.Audit.Model;
using Bluewire.NHibernate.Audit.Runtime;
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

        public Task OnFlushEntityAsync(FlushEntityEvent @event, CancellationToken cancellationToken)
        {
            OnFlushEntity(@event);
            return Task.CompletedTask;
        }

        public void OnFlushEntity(FlushEntityEvent @event)
        {
            if (!IsDirty(@event)) return;

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
            @event.Session.Save(auditEntry);
        }

        private bool IsDirty(FlushEntityEvent @event)
        {
            if (!@event.EntityEntry.ExistsInDatabase) return true;
            if (@event.HasDirtyCollection) return true;
            if (@event.DirtyProperties != null && @event.DirtyProperties.Any()) return true;
            return false;
        }

        public void OnDelete(DeleteEvent @event, ISet<object> transientEntities)
        {
            // This OnDelete overload is called when deleting orphaned entities, ie. when removing from
            // a one-to-many relation. Should be safe to delegate straight to the standard deletion behaviour.

            OnDelete(@event);
        }

        public Task OnDeleteAsync(DeleteEvent @event, CancellationToken cancellationToken)
        {
            OnDelete(@event);
            return Task.CompletedTask;
        }

        public Task OnDeleteAsync(DeleteEvent @event, ISet<object> transientEntities, CancellationToken cancellationToken)
        {
            OnDelete(@event, transientEntities);
            return Task.CompletedTask;
        }

        public void OnDelete(DeleteEvent @event)
        {
            var entity = @event.Session.PersistenceContext.UnproxyAndReassociate(@event.Entity);

            var persister = @event.Session.GetEntityPersister(@event.EntityName, entity);
            IAuditableEntityModel entityModel;
            if (!model.TryGetModelForPersister(persister, out entityModel)) return;

            var sessionAuditInfo = sessions.Lookup(@event.Session);

            var auditEntry = model.GenerateAuditEntry(entityModel, entity);
            auditEntry.AuditDatestamp = sessionAuditInfo.OperationDatestamp;

            AuditDelete(auditEntry, @event, persister);

            Debug.Assert(Equals(auditEntry.Id, persister.GetIdentifier(entity)));
            Debug.Assert(!Equals(auditEntry.VersionId, auditEntry.PreviousVersionId));
            @event.Session.Save(auditEntry);
        }

        private static void AuditAdd(IEntityAuditHistory auditEntry, FlushEntityEvent @event)
        {
            auditEntry.AuditedOperation = AuditedOperation.Add;
            auditEntry.PreviousVersionId = null;
            auditEntry.VersionId = @event.EntityEntry.Persister.GetVersion(@event.Entity);
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
            auditEntry.PreviousVersionId = persister.GetVersion(@event.Entity);
            auditEntry.VersionId = null;
        }
    }
}
