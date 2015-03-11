using System.Collections.Generic;

namespace Bluewire.NHibernate.Audit.Query
{
    public interface IMapRelationSnapshotQuery<TEntity, TEntityKey, TCollectionKey, TRelatedObject, TRelationValue>
        where TEntity : IEntityAuditHistory<TEntityKey>
    {
        ICollectionSnapshotQuery<TEntity, IDictionary<TCollectionKey, TRelatedObject>> Using<TRelation>() where TRelation : KeyedRelationAuditHistoryEntry<TEntityKey, TCollectionKey, TRelationValue>;
    }
}