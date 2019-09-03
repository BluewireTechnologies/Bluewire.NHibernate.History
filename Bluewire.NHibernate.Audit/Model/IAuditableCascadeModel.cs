using System;

namespace Bluewire.NHibernate.Audit.Model
{
    public interface IAuditableCascadeModel : IAuditRecordModel
    {
        /// <summary>
        /// The type of the parent entity.
        /// </summary>
        Type ParentType { get; }
        /// <summary>
        /// The entity name of the parent entity.
        /// </summary>
        string ParentEntityName { get; }
        /// <summary>
        /// The name of the property on the parent entity which references the child entity.
        /// </summary>
        string ReferencingProperty { get; }
        /// <summary>
        /// The type of the child entity.
        /// </summary>
        Type ChildType { get; }
    }
}
