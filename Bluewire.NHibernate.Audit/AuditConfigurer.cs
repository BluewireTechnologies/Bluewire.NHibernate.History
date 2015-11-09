using System.Linq;
using Bluewire.NHibernate.Audit.Listeners;
using Bluewire.NHibernate.Audit.Model;
using Bluewire.NHibernate.Audit.Runtime;
using NHibernate.Cfg;
using NHibernate.Event;
using System;
using NHibernate;
using System.Diagnostics;

namespace Bluewire.NHibernate.Audit
{
    public class AuditConfigurer
    {
        private readonly IAuditEntryFactory auditEntryFactory;
        private readonly IAuditDatestampProvider datestampProvider;

        public AuditConfigurer(IAuditEntryFactory auditEntryFactory, IAuditDatestampProvider datestampProvider)
        {
            this.auditEntryFactory = auditEntryFactory;
            this.datestampProvider = datestampProvider;
        }

        public IAuditInfo IntegrateWithNHibernate(Configuration cfg)
        {
            return this.IntegrateWithNHibernate(cfg, cfg.EventListeners);
        }

        public IAuditInfo IntegrateWithNHibernate(Configuration cfg, EventListeners eventListeners)
        {
            var modelBuilder = new AuditModelBuilder();
            modelBuilder.AddFromConfiguration(cfg);
            var model = modelBuilder.GetValidatedModel(auditEntryFactory);

            var sessions = new SessionsAuditInfo(datestampProvider);
            RegisterEventListeners(eventListeners, model, sessions);

            return new NHibernateAuditIntegrationInstance(sessions);
        }

        class NHibernateAuditIntegrationInstance : IAuditInfo
        {
            private SessionsAuditInfo sessions;

            public NHibernateAuditIntegrationInstance(SessionsAuditInfo sessions)
            {
                this.sessions = sessions;
            }

            public DateTimeOffset CommitSnapshot(ISession session)
            {
                var sessionAuditInfo = sessions.Lookup(session.GetSessionImplementation());
                if (sessionAuditInfo.IsFlushing) throw new InvalidOperationException("A session flush is currently in progress. This method cannot be called during a flush, eg. from an NHibernate event listener.");
                sessionAuditInfo.BeginFlush();
                try
                {
                    Debug.Assert(sessionAuditInfo.IsFlushing);
                    session.Flush();
                    Debug.Assert(sessionAuditInfo.IsFlushing);
                    return sessionAuditInfo.OperationDatestamp;
                }
                finally
                {
                    sessionAuditInfo.EndFlush();
                }
            }
        }

        private static void RegisterEventListeners(EventListeners listeners, AuditModel model, SessionsAuditInfo sessions)
        {
            var concurrencyCheckListener = new []{  new ConcurrencyCheckShouldPreferObjectVersionOverSessionRecordedVersion() };
            listeners.SaveEventListeners = concurrencyCheckListener.Concat(listeners.SaveEventListeners).ToArray();
            listeners.SaveOrUpdateEventListeners = concurrencyCheckListener.Concat(listeners.SaveOrUpdateEventListeners).ToArray();
            listeners.FlushEntityEventListeners = concurrencyCheckListener.Concat(listeners.FlushEntityEventListeners).ToArray();

            listeners.FlushEventListeners = new []{ new BeforeFlush(sessions) }.Concat(listeners.FlushEventListeners).Concat(new [] { new AfterFlush(sessions) }).ToArray();

            var auditListener = new AuditEntityListener(sessions, model);
            listeners.FlushEntityEventListeners = listeners.FlushEntityEventListeners.Concat(new [] { auditListener }).ToArray();
            listeners.DeleteEventListeners = listeners.DeleteEventListeners.Concat(new[] { auditListener }).ToArray();

            var collectionAuditListener = new AuditCollectionListener(
                new AuditValueCollectionListener(sessions, model));
            listeners.PreCollectionRecreateEventListeners = listeners.PreCollectionRecreateEventListeners.Concat(new[] { collectionAuditListener }).ToArray();
            listeners.PreCollectionRemoveEventListeners = listeners.PreCollectionRemoveEventListeners.Concat(new[] { collectionAuditListener }).ToArray();
            listeners.PreCollectionUpdateEventListeners = listeners.PreCollectionUpdateEventListeners.Concat(new[] { collectionAuditListener }).ToArray();
        }
    }

}
