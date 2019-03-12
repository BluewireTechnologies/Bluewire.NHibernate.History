using System;
using System.Linq;
using System.Linq.Expressions;
using Bluewire.NHibernate.Audit.Query.Internal;

namespace Bluewire.NHibernate.Audit.Query.Model
{
    public class EntitySnapshotQueryModel<TEntity, TEntityKey> : IEntitySnapshotQuery<TEntity, TEntityKey> where TEntity : IEntityAuditHistory<TEntityKey>
    {
        private readonly ISnapshotContext context;

        public EntitySnapshotQueryModel(ISnapshotContext context)
        {
            this.context = context;
        }

        public TEntity Get(TEntityKey id)
        {
            var xParam = Expression.Parameter(typeof(TEntity), "x");
            var idMatches = Expression.Lambda<Func<TEntity, bool>>(
                Expression.Equal(
                    Expression.Property(xParam, "Id"),
                    Expression.Constant(id)),
                xParam);

            return AuditQueryHelper.QueryEntity<TEntity, TEntityKey>(context).Where(idMatches).SingleOrDefault();
        }

        public ILookup<TEntityKey, TEntity> GetMany(params TEntityKey[] ids)
        {
            return AuditQueryHelper.QueryEntity<TEntity, TEntityKey>(context).Where(e => ids.Contains(e.Id)).ToLookup(e => e.Id);
        }

        public ComponentMapRelationSnapshotQueryModel<TEntity, TEntityKey, TCollectionKey, TValue> QueryMapOf<TCollectionKey, TValue>()
        {
            return new ComponentMapRelationSnapshotQueryModel<TEntity, TEntityKey, TCollectionKey, TValue>(context);
        }

        public EntityMapRelationSnapshotQueryModel<TEntity, TEntityKey, TCollectionKey, TRelatedEntity, TRelatedEntityKey> QueryMapOf<TCollectionKey, TRelatedEntity, TRelatedEntityKey>()
            where TRelatedEntity : IEntityAuditHistory<TRelatedEntityKey>
        {
            return new EntityMapRelationSnapshotQueryModel<TEntity, TEntityKey, TCollectionKey, TRelatedEntity, TRelatedEntityKey>(context);
        }

        public ComponentListRelationSnapshotQueryModel<TEntity, TEntityKey, TValue> QueryListOf<TValue>()
        {
            return new ComponentListRelationSnapshotQueryModel<TEntity, TEntityKey, TValue>(context);
        }

        public EntityListRelationSnapshotQueryModel<TEntity, TEntityKey, TRelatedEntity, TRelatedEntityKey> QueryListOf<TRelatedEntity, TRelatedEntityKey>()
            where TRelatedEntity : IEntityAuditHistory<TRelatedEntityKey>
        {
            return new EntityListRelationSnapshotQueryModel<TEntity, TEntityKey, TRelatedEntity, TRelatedEntityKey>(context);
        }

        public ComponentSetRelationSnapshotQueryModel<TEntity, TEntityKey, TValue> QuerySetOf<TValue>()
        {
            return new ComponentSetRelationSnapshotQueryModel<TEntity, TEntityKey, TValue>(context);
        }

        public EntitySetRelationSnapshotQueryModel<TEntity, TEntityKey, TRelatedEntity, TRelatedEntityKey> QuerySetOf<TRelatedEntity, TRelatedEntityKey>()
            where TRelatedEntity : IEntityAuditHistory<TRelatedEntityKey>
        {
            return new EntitySetRelationSnapshotQueryModel<TEntity, TEntityKey, TRelatedEntity, TRelatedEntityKey>(context);
        }

        public IQueryable<TEntity> Query
        {
            get { return AuditQueryHelper.QueryEntity<TEntity, TEntityKey>(context); }
        }
    }
}
