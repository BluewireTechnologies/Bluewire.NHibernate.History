using NHibernate.Event;

namespace Bluewire.NHibernate.Audit.Listeners
{
    class AfterFlush : IFlushEventListener
    {
        public void OnFlush(FlushEvent @event)
        {
            SessionAuditInfo.For(@event.Session).EndFlush();
        }
    }
}