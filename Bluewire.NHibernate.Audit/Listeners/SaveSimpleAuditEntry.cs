﻿using System.Diagnostics;
using System.Linq;
using Bluewire.NHibernate.Audit.Model;
using NHibernate;
using NHibernate.Event;

namespace Bluewire.NHibernate.Audit.Listeners
{
    class SaveSimpleAuditEntry : IFlushEntityEventListener, IDeleteEventListener
    {
        private readonly AuditModel model;

        public SaveSimpleAuditEntry(AuditModel model)
        {
            this.model = model;
        }

        public void OnFlushEntity(FlushEntityEvent @event)
        {
            if (@event.EntityEntry.ExistsInDatabase && (@event.DirtyProperties == null || !@event.DirtyProperties.Any())) return;

            IAuditableEntityModel entityModel;
            if (!model.TryGetModelForPersister(@event.EntityEntry.Persister, out entityModel)) return;

            var sessionAuditInfo = SessionAuditInfo.For(@event.Session);

            var auditEntry = model.GenerateAuditEntry(entityModel, @event.Entity);
            auditEntry.AuditDatestamp = sessionAuditInfo.FlushDatestamp;
            if (@event.EntityEntry.ExistsInDatabase)
            {
                auditEntry.PreviousVersionId = @event.EntityEntry.Version;
                auditEntry.VersionId = @event.PropertyValues[@event.EntityEntry.Persister.VersionProperty];
                auditEntry.AuditedOperation = AuditedOperation.Update;
            }
            else
            {
                auditEntry.PreviousVersionId = null;
                auditEntry.VersionId = @event.EntityEntry.Persister.GetVersion(@event.Entity, EntityMode.Poco);
                auditEntry.AuditedOperation = AuditedOperation.Add;
            }
            Debug.Assert(Equals(auditEntry.Id, @event.EntityEntry.EntityKey.Identifier));
            Debug.Assert(!Equals(auditEntry.VersionId, auditEntry.PreviousVersionId));
            @event.Session.Save(auditEntry);
        }

        public void OnDelete(DeleteEvent @event, Iesi.Collections.ISet transientEntities)
        {
            //OnDelete(@event);
        }

        public void OnDelete(DeleteEvent @event)
        {
            var persister = @event.Session.GetEntityPersister(@event.EntityName, @event.Entity);
            IAuditableEntityModel entityModel;
            if (!model.TryGetModelForPersister(persister, out entityModel)) return;

            var sessionAuditInfo = SessionAuditInfo.For(@event.Session);

            var auditEntry = model.GenerateAuditEntry(entityModel, @event.Entity);
            auditEntry.AuditDatestamp = sessionAuditInfo.OperationDatestamp;
            
            auditEntry.PreviousVersionId = persister.GetVersion(@event.Entity, EntityMode.Poco);
            auditEntry.VersionId = null;
            auditEntry.AuditedOperation = AuditedOperation.Delete;

            Debug.Assert(Equals(auditEntry.Id, persister.GetIdentifier(@event.Entity, EntityMode.Poco)));
            Debug.Assert(!Equals(auditEntry.VersionId, auditEntry.PreviousVersionId));
            @event.Session.Save(auditEntry);
        }
    }
}