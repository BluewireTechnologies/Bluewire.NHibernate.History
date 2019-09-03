using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Bluewire.NHibernate.Audit.Meta;
using Bluewire.NHibernate.Audit.Model;
using Bluewire.NHibernate.Audit.Runtime;
using NHibernate;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Persister.Entity;

namespace Bluewire.NHibernate.Audit.Listeners
{
    class AuditEntityListener : IFlushEntityEventListener, IDeleteEventListener, IDirtyCheckEventListener, IFlushEventListener, IAutoFlushEventListener
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
            if (!IsDirty(@event)) return;

            IAuditableEntityModel entityModel;
            if (!model.TryGetModelForPersister(@event.EntityEntry.Persister, out entityModel))
            {
                //var entry = @event.Session.PersistenceContext.GetEntry(@event.Entity);
                //CheckInverseCascades(@event.Session, entry, @event.Entity);
                return;
            }

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
            if (@event.EntityEntry.LockMode == LockMode.Force) return true;
            return false;
        }

        public void OnDelete(DeleteEvent @event, ISet<object> transientEntities)
        {
            // This OnDelete overload is called when deleting orphaned entities, ie. when removing from
            // a one-to-many relation. Should be safe to delegate straight to the standard deletion behaviour.

            OnDelete(@event);
        }

        private void CheckInverseCascades(IEventSource session, EntityEntry entry, object entity)
        {
            foreach (var cascade in model.EnumerateInverseCascades(entity.GetType()))
            {
                var parent = session.Get(cascade.ParentEntityName, entry.EntityKey.Identifier);
                if (parent == null) continue;

                var parentEntry = session.PersistenceContext.GetEntry(parent);
                if (parentEntry.ExistsInDatabase && parentEntry.Status != Status.Deleted)
                {
                    parentEntry.LockMode = LockMode.Force;
                    // Nulling out the version in the loaded state will cause the entity to appear dirty
                    // to NHibernate, but won't interfere with any audit code which needs to read this.
                    parentEntry.LoadedState[parentEntry.Persister.VersionProperty] = null;
                }
            }
        }

        public void OnDelete(DeleteEvent @event)
        {
            if (@event.EntityName != null)
            {
                var entry = @event.Session.PersistenceContext.GetEntry(@event.Entity);
                // Cascade-delete of a one-to-one-related entity can cause OnDelete to be called
                // with the parent's EntityName but the child entity instance.
                if (entry?.EntityName != @event.EntityName)
                {
                    CheckInverseCascades(@event.Session, entry, @event.Entity);
                    return;
                }
            }

            var entity = @event.Session.PersistenceContext.UnproxyAndReassociate(@event.Entity);

            var persister = @event.Session.GetEntityPersister(@event.EntityName, entity);
            IAuditableEntityModel entityModel;
            if (!model.TryGetModelForPersister(persister, out entityModel)) return;

            var sessionAuditInfo = sessions.Lookup(@event.Session);

            var auditEntry = model.GenerateAuditEntry(entityModel, entity);
            auditEntry.AuditDatestamp = sessionAuditInfo.OperationDatestamp;

            AuditDelete(auditEntry, @event, persister);

            Debug.Assert(Equals(auditEntry.Id, persister.GetIdentifier(entity, EntityMode.Poco)));
            Debug.Assert(!Equals(auditEntry.VersionId, auditEntry.PreviousVersionId));
            @event.Session.Save(auditEntry);
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

        public void OnDirtyCheck(DirtyCheckEvent @event) => OnFlush(@event);
        public void OnAutoFlush(AutoFlushEvent @event) => OnFlush(@event);

        public void OnFlush(FlushEvent @event)
        {
            foreach (var entity in @event.Session.PersistenceContext.EntitiesByKey.Values)
            {
                var entry = @event.Session.PersistenceContext.GetEntry(entity);
                CheckInverseCascades(@event.Session, entry, entity);
            }
        }
    }
}
