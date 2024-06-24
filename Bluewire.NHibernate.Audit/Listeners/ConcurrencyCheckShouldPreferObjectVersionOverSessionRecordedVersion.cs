using System.Threading;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Engine;
using NHibernate.Event;

namespace Bluewire.NHibernate.Audit.Listeners
{
    /// <summary>
    /// See http://stackingcode.com/blog/2010/12/09/optimistic-concurrency-and-nhibernate. If you fetch an entity, update
    /// it from a DTO, and set its version to that of the DTO, NHibernate will ignore the version you specified and use the
    /// one it read from the database, cheerfully overwriting someone else's changes.
    /// </summary>
    class ConcurrencyCheckShouldPreferObjectVersionOverSessionRecordedVersion : ISaveOrUpdateEventListener, IFlushEntityEventListener
    {
        public Task OnSaveOrUpdateAsync(SaveOrUpdateEvent @event, CancellationToken cancellationToken)
        {
            OnSaveOrUpdate(@event);
            return Task.CompletedTask;
        }

        public void OnSaveOrUpdate(SaveOrUpdateEvent @event)
        {
            if (@event.Entry == null) return;
            CheckEntityVersion(@event.Session, @event.Entity, @event.Entry, @event.RequestedId);
        }

        public Task OnFlushEntityAsync(FlushEntityEvent @event, CancellationToken cancellationToken)
        {
            OnFlushEntity(@event);
            return Task.CompletedTask;
        }

        public void OnFlushEntity(FlushEntityEvent @event)
        {
            CheckEntityVersion(@event.Session, @event.Entity, @event.EntityEntry, @event.EntityEntry.Id);
        }

        private void CheckEntityVersion(ISessionImplementor session, object entity, EntityEntry entry, object id)
        {
            var entityPersister = session.GetEntityPersister(entry.EntityName, entity);
            if (!entityPersister.IsVersioned) return;

            var version = entityPersister.GetVersion(entity);

            if (entityPersister.VersionType.IsEqual(version, entry.Version)) return;

            throw new StaleObjectStateException(entityPersister.EntityName, id);
        }
    }
}
