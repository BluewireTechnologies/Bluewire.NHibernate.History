using System.Collections.Generic;

namespace Bluewire.NHibernate.Audit.Query
{
    public interface ISetRelationSnapshotQuery<TEntity, TEntityKey, TRelatedObject, TRelationValue>
        where TEntity : IEntityAuditHistory<TEntityKey>
    {
        ICollectionSnapshotQuery<TEntity, ICollection<TRelatedObject>> Using<TRelation>() where TRelation : SetRelationAuditHistoryEntry<TEntityKey, TRelationValue>;
    }
}
