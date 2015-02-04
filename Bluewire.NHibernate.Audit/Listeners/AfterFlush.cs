using Bluewire.NHibernate.Audit.Runtime;
using NHibernate.Event;

namespace Bluewire.NHibernate.Audit.Listeners
{
    class AfterFlush : IFlushEventListener
    {
        private readonly SessionsAuditInfo sessions;

        public AfterFlush(SessionsAuditInfo sessions)
        {
            this.sessions = sessions;
        }

        public void OnFlush(FlushEvent @event)
        {
            sessions.Lookup(@event.Session).EndFlush(@event.Session);
        }
    }
}