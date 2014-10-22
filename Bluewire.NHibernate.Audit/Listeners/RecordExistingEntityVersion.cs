using Bluewire.NHibernate.Audit.Model;
using NHibernate;
using NHibernate.Event;

namespace Bluewire.NHibernate.Audit.Listeners
{
    class RecordExistingEntityVersion : IFlushEntityEventListener
    {
        private readonly AuditModel model;

        public RecordExistingEntityVersion(AuditModel model)
        {
            this.model = model;
        }

        public void OnFlushEntity(FlushEntityEvent @event)
        {
            IAuditableEntityModel entityModel;
            if (!model.TryGetModelForPersister(@event.EntityEntry.Persister, out entityModel)) return;

            var state = SessionAuditInfo.For(@event.Session).GetState(@event.Entity);
            state.PreviousVersionId = @event.EntityEntry.Version;
        }
    }
}