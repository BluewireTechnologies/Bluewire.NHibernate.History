﻿using System.Threading;
using System.Threading.Tasks;
using Bluewire.NHibernate.Audit.Runtime;
using NHibernate.Event;

namespace Bluewire.NHibernate.Audit.Listeners
{
    class BeforeFlush : IFlushEventListener
    {
        private readonly SessionsAuditInfo sessions;

        public BeforeFlush(SessionsAuditInfo sessions)
        {
            this.sessions = sessions;
        }

        public Task OnFlushAsync(FlushEvent @event, CancellationToken cancellationToken)
        {
            OnFlush(@event);
            return Task.CompletedTask;
        }

        public void OnFlush(FlushEvent @event)
        {
            sessions.Lookup(@event.Session).BeginFlush();
        }
    }
}
