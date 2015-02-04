using NHibernate;
using NHibernate.Engine;

namespace Bluewire.NHibernate.Audit.Runtime
{
    public interface IWorkItem
    {
        void Execute(ISessionImplementor session, ISessionSnapshot snapshot);
    }
}