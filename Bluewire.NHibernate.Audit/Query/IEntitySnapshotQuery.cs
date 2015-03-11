using System.Linq;
using Bluewire.NHibernate.Audit.Query.Model;

namespace Bluewire.NHibernate.Audit.Query
{
    public interface IEntitySnapshotQuery<TEntity, TEntityKey> where TEntity : IEntityAuditHistory<TEntityKey>
    {
        TEntity Get(TEntityKey id);
        ILookup<TEntityKey, TEntity> GetMany(params TEntityKey[] ids);

        ComponentMapRelationSnapshotQueryModel<TEntity, TEntityKey, TCollectionKey, TValue> QueryMapOf<TCollectionKey, TValue>();

        EntityMapRelationSnapshotQueryModel<TEntity, TEntityKey, TCollectionKey, TRelatedEntity, TRelatedEntityKey> QueryMapOf<TCollectionKey, TRelatedEntity, TRelatedEntityKey>()
            where TRelatedEntity : IEntityAuditHistory<TRelatedEntityKey>;

        ComponentListRelationSnapshotQueryModel<TEntity, TEntityKey, TValue> QueryListOf<TValue>();

        EntityListRelationSnapshotQueryModel<TEntity, TEntityKey, TRelatedEntity, TRelatedEntityKey> QueryListOf<TRelatedEntity, TRelatedEntityKey>()
            where TRelatedEntity : IEntityAuditHistory<TRelatedEntityKey>;

        ComponentSetRelationSnapshotQueryModel<TEntity, TEntityKey, TValue> QuerySetOf<TValue>();

        EntitySetRelationSnapshotQueryModel<TEntity, TEntityKey, TRelatedEntity, TRelatedEntityKey> QuerySetOf<TRelatedEntity, TRelatedEntityKey>()
            where TRelatedEntity : IEntityAuditHistory<TRelatedEntityKey>;

        IQueryable<TEntity> Query { get; }
    }
}