using System.Linq;
using Bluewire.Common.Extensions;
using NHibernate.Event;

namespace Bluewire.NHibernate.Audit.Listeners
{
    class BeforeFlush : IFlushEventListener
    {
        public void OnFlush(FlushEvent @event)
        {
            SessionAuditInfo.For(@event.Session).BeginFlush();
        }
    }
}