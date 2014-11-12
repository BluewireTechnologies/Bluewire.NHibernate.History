using NHibernate.Engine;

namespace Bluewire.NHibernate.Audit.Model
{
    /// <summary>
    /// The identity of a value type is its value.
    /// </summary>
    class ValueTypeIdentityResolver : IElementIdentityResolver
    {
        public object Resolve(object collectionElement, ISessionImplementor session)
        {
            return collectionElement;
        }
    }
}