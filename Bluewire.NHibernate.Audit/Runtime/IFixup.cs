using NHibernate.Engine;

namespace Bluewire.NHibernate.Audit.Runtime
{
    public interface IFixup
    {
        void Apply(ISessionImplementor session, SessionAuditInfo sessionAuditInfo);
    }
}