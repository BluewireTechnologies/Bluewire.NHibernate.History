using System.Linq;
using Bluewire.Common.Extensions;
using Bluewire.Common.Time;
using Bluewire.NHibernate.Audit.Listeners;
using Bluewire.NHibernate.Audit.Model;
using NHibernate.Cfg;
using NHibernate.Event;

namespace Bluewire.NHibernate.Audit
{
    public class AuditConfigurer
    {
        private readonly IAuditEntryFactory auditEntryFactory;
        private readonly IClock clock;

        public AuditConfigurer(IAuditEntryFactory auditEntryFactory) : this(auditEntryFactory, new Clock())
        {
        }

        public AuditConfigurer(IAuditEntryFactory auditEntryFactory, IClock clock)
        {
            this.auditEntryFactory = auditEntryFactory;
            this.clock = clock;
        }

        public void IntegrateWithNHibernate(Configuration cfg)
        {
            cfg.BuildMappings(); // Must do this first, to get collection information.

            var modelBuilder = new AuditModelBuilder();
            modelBuilder.AddFromConfiguration(cfg);
            var model = modelBuilder.GetValidatedModel(auditEntryFactory);

            var sessions = new SessionsAuditInfo(clock);
            RegisterEventListeners(cfg.EventListeners, model, sessions);
        }

        private static void RegisterEventListeners(EventListeners listeners, AuditModel model, SessionsAuditInfo sessions)
        {
            var concurrencyCheckListener = new ConcurrencyCheckShouldPreferObjectVersionOverSessionRecordedVersion();
            listeners.SaveEventListeners = listeners.SaveEventListeners.Prepend(concurrencyCheckListener).ToArray();
            listeners.SaveOrUpdateEventListeners = listeners.SaveOrUpdateEventListeners.Prepend(concurrencyCheckListener).ToArray();
            listeners.FlushEntityEventListeners = listeners.FlushEntityEventListeners.Prepend(concurrencyCheckListener).ToArray();

            listeners.FlushEventListeners = listeners.FlushEventListeners.Prepend(new BeforeFlush(sessions)).Append(new AfterFlush(sessions)).ToArray();

            var auditListener = new AuditEntityListener(sessions, model);
            listeners.FlushEntityEventListeners = listeners.FlushEntityEventListeners.Append(auditListener).ToArray();
            listeners.DeleteEventListeners = listeners.DeleteEventListeners.Append(auditListener).ToArray();

            // Deletion, then insertion.
            var collectionAuditListeners = new AuditCollectionListenerBase []{
                new AuditCollectionElementDeletionListener(sessions, model),
                new AuditCollectionElementInsertionListener(sessions, model)
            };
            listeners.PreCollectionRecreateEventListeners = listeners.PreCollectionRecreateEventListeners.Concat(collectionAuditListeners).ToArray();
            listeners.PreCollectionRemoveEventListeners = listeners.PreCollectionRemoveEventListeners.Concat(collectionAuditListeners).ToArray();
            listeners.PreCollectionUpdateEventListeners = listeners.PreCollectionUpdateEventListeners.Concat(collectionAuditListeners).ToArray();
        }

    }

}
