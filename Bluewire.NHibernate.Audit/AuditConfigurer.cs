using System.Linq;
using System.Text;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Engine;
using NHibernate.Event;

namespace Bluewire.NHibernate.Audit
{
    public class AuditConfigurer
    {
        public void IntegrateWithNHibernate(Configuration cfg)
        {
            RegisterEventListeners(cfg.EventListeners);
        }

        private static void RegisterEventListeners(EventListeners listeners)
        {
            var concurrencyCheckListener = new ConcurrencyCheckShouldPreferObjectVersionOverSessionRecordedVersion();
            listeners.FlushEntityEventListeners = new[] { concurrencyCheckListener }.Concat(listeners.FlushEntityEventListeners).ToArray();
            listeners.SaveEventListeners = new[] { concurrencyCheckListener }.Concat(listeners.SaveEventListeners).ToArray();
            listeners.SaveOrUpdateEventListeners = new[] { concurrencyCheckListener }.Concat(listeners.SaveOrUpdateEventListeners).ToArray();
        }

        /// <summary>
        /// See http://stackingcode.com/blog/2010/12/09/optimistic-concurrency-and-nhibernate. If you fetch an entity, update
        /// it from a DTO, and set its version to that of the DTO, NHibernate will ignore the version you specified and use the
        /// one it read from the database, cheerfully overwriting someone else's changes.
        /// </summary>
        class ConcurrencyCheckShouldPreferObjectVersionOverSessionRecordedVersion : ISaveOrUpdateEventListener, IFlushEntityEventListener
        {
            public void OnSaveOrUpdate(SaveOrUpdateEvent @event)
            {
                if (@event.Entry == null) return;
                CheckEntityVersion(@event.Session, @event.Entity, @event.Entry, @event.RequestedId);
            }

            public void OnFlushEntity(FlushEntityEvent @event)
            {
                CheckEntityVersion(@event.Session, @event.Entity, @event.EntityEntry, @event.EntityEntry.Id);
            }

            private void CheckEntityVersion(ISessionImplementor session, object entity, EntityEntry entry, object id)
            {
                var entityPersister = session.GetEntityPersister(null, entity);
                if (!entityPersister.IsVersioned) return;
                
                var version = entityPersister.GetVersion(entity, session.EntityMode);

                if (entityPersister.VersionType.IsEqual(version, entry.Version)) return;
                
                throw new StaleObjectStateException(entityPersister.EntityName, id);
            }
        }

    }
}
