using System.Collections.Generic;

namespace Bluewire.NHibernate.Audit.Query
{
    public interface IListRelationSnapshotQuery<TEntity, TEntityKey, TRelatedObject, TRelationValue>
        where TEntity : IEntityAuditHistory<TEntityKey>
    {
        ICollectionSnapshotQuery<TEntity, IList<TRelatedObject>> Using<TRelation>() where TRelation : KeyedRelationAuditHistoryEntry<TEntityKey, int, TRelationValue>;
    }
}