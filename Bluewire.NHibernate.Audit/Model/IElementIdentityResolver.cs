using NHibernate.Engine;

namespace Bluewire.NHibernate.Audit.Model
{
    public interface IElementIdentityResolver
    {
        object Resolve(object collectionElement, ISessionImplementor session);
    }
}