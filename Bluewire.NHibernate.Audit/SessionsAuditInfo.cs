using Bluewire.Common.Time;
using Bluewire.NHibernate.Audit.Support;
using NHibernate.Engine;

namespace Bluewire.NHibernate.Audit
{
    public class SessionsAuditInfo
    {
        private readonly IClock clock;

        public SessionsAuditInfo(IClock clock)
        {
            this.clock = clock;
        }

        private readonly WeakDictionary<ISessionImplementor, SessionAuditInfo> sessionInfos = new WeakDictionary<ISessionImplementor, SessionAuditInfo>();

        public SessionAuditInfo Lookup(ISessionImplementor session)
        {
            return sessionInfos.GetOrAdd(session, () => new SessionAuditInfo(clock));
        }

    }
}