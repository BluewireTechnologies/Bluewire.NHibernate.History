using Bluewire.NHibernate.Audit.Support;
using NHibernate.Engine;

namespace Bluewire.NHibernate.Audit.Runtime
{
    public class SessionsAuditInfo
    {
        private readonly IAuditDatestampProvider datestampProvider;

        public SessionsAuditInfo(IAuditDatestampProvider datestampProvider)
        {
            this.datestampProvider = datestampProvider;
        }

        private readonly WeakDictionary<ISessionImplementor, SessionAuditInfo> sessionInfos = new WeakDictionary<ISessionImplementor, SessionAuditInfo>();

        public SessionAuditInfo Lookup(ISessionImplementor session)
        {
            return sessionInfos.GetOrAdd(session, () => new SessionAuditInfo(datestampProvider));
        }

    }
}