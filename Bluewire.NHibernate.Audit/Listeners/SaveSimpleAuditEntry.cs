using System.Diagnostics;
using System.Linq;
using Bluewire.NHibernate.Audit.Model;
using NHibernate;
using NHibernate.Event;

namespace Bluewire.NHibernate.Audit.Listeners
{
    class SaveSimpleAuditEntry : IFlushEntityEventListener
    {
        private readonly AuditModel model;

        public SaveSimpleAuditEntry(AuditModel model)
        {
            this.model = model;
        }

        public void OnFlushEntity(FlushEntityEvent @event)
        {
            if (@event.EntityEntry.ExistsInDatabase && (@event.DirtyProperties == null || !@event.DirtyProperties.Any())) return;

            var operation = @event.EntityEntry.ExistsInDatabase ? AuditedOperation.Update : AuditedOperation.Add;

            IAuditableEntityModel entityModel;
            if (!model.TryGetModelForPersister(@event.EntityEntry.Persister, out entityModel)) return;

            var sessionAuditInfo = SessionAuditInfo.For(@event.Session);

            var state = sessionAuditInfo.GetState(@event.Entity);
            var auditEntry = model.GenerateAuditEntry(entityModel, @event.Entity);
            Debug.Assert(Equals(auditEntry.Id, @event.EntityEntry.EntityKey.Identifier));
            auditEntry.PreviousVersionId = state.PreviousVersionId;
            auditEntry.AuditDatestamp = sessionAuditInfo.FlushDatestamp;
            auditEntry.AuditedOperation = operation;
            @event.Session.Save(auditEntry);
        }
    }
}