using System;
using System.Linq;
using System.Text;
using Bluewire.Common.Extensions;
using Bluewire.NHibernate.Audit.Listeners;
using Bluewire.NHibernate.Audit.Model;
using NHibernate.Cfg;
using NHibernate.Event;

namespace Bluewire.NHibernate.Audit
{
    public class AuditConfigurer
    {
        private readonly IAuditEntryFactory auditEntryFactory;

        public AuditConfigurer(IAuditEntryFactory auditEntryFactory)
        {
            this.auditEntryFactory = auditEntryFactory;
        }

        public void IntegrateWithNHibernate(Configuration cfg)
        {
            var modelBuilder = new AuditModelBuilder();
            modelBuilder.AddFromConfiguration(cfg);
            var model = modelBuilder.GetValidatedModel(auditEntryFactory);

            RegisterEventListeners(cfg.EventListeners, model);
        }

        private static void RegisterEventListeners(EventListeners listeners, AuditModel model)
        {
            var concurrencyCheckListener = new ConcurrencyCheckShouldPreferObjectVersionOverSessionRecordedVersion();
            listeners.SaveEventListeners = listeners.SaveEventListeners.Prepend(concurrencyCheckListener).ToArray();
            listeners.SaveOrUpdateEventListeners = listeners.SaveOrUpdateEventListeners.Prepend(concurrencyCheckListener).ToArray();
            listeners.FlushEntityEventListeners = listeners.FlushEntityEventListeners.Prepend(concurrencyCheckListener).ToArray();

            listeners.FlushEventListeners = listeners.FlushEventListeners.Prepend(new BeforeFlush()).Append(new AfterFlush()).ToArray();
            listeners.FlushEntityEventListeners = listeners.FlushEntityEventListeners.Prepend(new RecordExistingEntityVersion(model)).Append(new SaveSimpleAuditEntry(model)).ToArray();
        }

    }

}
