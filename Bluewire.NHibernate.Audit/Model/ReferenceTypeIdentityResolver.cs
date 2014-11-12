using NHibernate.Engine;
using NHibernate.Type;

namespace Bluewire.NHibernate.Audit.Model
{
    /// <summary>
    /// The identity of a reference type is its ID.
    /// </summary>
    class ReferenceTypeIdentityResolver : IElementIdentityResolver
    {
        private readonly ManyToOneType type;

        public ReferenceTypeIdentityResolver(ManyToOneType type)
        {
            this.type = type;
        }

        public object Resolve(object collectionElement, ISessionImplementor session)
        {
            return ForeignKeys.GetEntityIdentifierIfNotUnsaved(type.GetAssociatedEntityName(), collectionElement, session);
        }
    }
}