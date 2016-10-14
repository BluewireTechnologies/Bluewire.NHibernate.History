using System.Runtime.CompilerServices;
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

        private readonly ConditionalWeakTable<ISessionImplementor, SessionAuditInfo> sessionInfos = new ConditionalWeakTable<ISessionImplementor, SessionAuditInfo>();

        public SessionAuditInfo Lookup(ISessionImplementor session)
        {
            return sessionInfos.GetValue(session, s => new SessionAuditInfo(datestampProvider));
        }
    }
}
