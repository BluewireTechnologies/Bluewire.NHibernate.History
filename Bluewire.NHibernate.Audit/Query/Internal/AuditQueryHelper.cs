using System;
using System.Linq;
using System.Linq.Expressions;
using Bluewire.NHibernate.Audit.Meta;

namespace Bluewire.NHibernate.Audit.Query.Internal
{
    public static class AuditQueryHelper
    {
        public static IQueryable<TEntity> QueryEntity<TEntity, TEntityKey>(ISnapshotContext snapshot) where TEntity : IEntityAuditHistory<TEntityKey>
        {
            return snapshot.Of<TEntity>().Where(NotSupersededByCorrelatedRecordExpression(snapshot.Of<TEntity>()));
        }

        /// <summary>
        /// Generate an expression representing the generic form of:
        ///  x => !ys.Where(y => y.Id == x.Id).Any(y => y.PreviousVersionId == x.Id)
        /// 
        /// This cannot be written in code since '==' is not defined for an unqualified generic type.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="ys"></param>
        /// <returns></returns>
        private static Expression<Func<TEntity, bool>> NotSupersededByCorrelatedRecordExpression<TEntity>(IQueryable<TEntity> ys)
        {
            var ysConst = Expression.Constant(ys);
            var xParam = Expression.Parameter(typeof(TEntity), "x");
            var yParam = Expression.Parameter(typeof(TEntity), "y");

            var isSuperseding = Expression.Lambda<Func<TEntity, bool>>(
                Expression.Equal(Expression.Property(yParam, "PreviousVersionId"), Expression.Property(xParam, "VersionId")),
                yParam);

            var isCorrelated = Expression.Lambda<Func<TEntity, bool>>(
                Expression.Equal(Expression.Property(yParam, "Id"), Expression.Property(xParam, "Id")),
                yParam);

            return
                Expression.Lambda<Func<TEntity, bool>>(
                    Expression.Not(
                        Expression.Call(typeof(Queryable), "Any", new[] { typeof(TEntity) },
                            Expression.Call(typeof(Queryable), "Where", new[] { typeof(TEntity) },
                                ysConst,
                                isCorrelated),
                            isSuperseding)
                    ),
                    xParam);
        }


        /// <summary>
        /// Returns all records of type T visible in the specified snapshot. This includes superseded records.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="snapshot"></param>
        /// <returns></returns>
        private static IQueryable<T> Of<T>(this ISnapshotContext snapshot) where T : IEntityAuditHistory
        {
            return snapshot.QueryableAudit<T>().Where(x => x.AuditDatestamp <= snapshot.SnapshotDatestamp);
        }


        public static IQueryable<TRelation> GetSetRelationSnapshot<TRelation, TEntityKey, TValue>(ISnapshotContext snapshot, TEntityKey[] ownerKeys) where TRelation : SetRelationAuditHistoryEntry<TEntityKey, TValue>
        {
            return snapshot.QueryableAudit<TRelation>()
                .Where(x => ownerKeys.Contains(x.OwnerId))
                .Where(x => x.StartDatestamp <= snapshot.SnapshotDatestamp && (x.EndDatestamp ?? DateTimeOffset.MaxValue) > snapshot.SnapshotDatestamp);
        }

        public static IQueryable<TRelation> GetKeyedRelationSnapshot<TRelation, TEntityKey, TCollectionKey, TValue>(ISnapshotContext snapshot, TEntityKey[] ownerKeys) where TRelation : KeyedRelationAuditHistoryEntry<TEntityKey, TCollectionKey, TValue>
        {
            return snapshot.QueryableAudit<TRelation>()
                .Where(x => ownerKeys.Contains(x.OwnerId))
                .Where(x => x.StartDatestamp <= snapshot.SnapshotDatestamp && (x.EndDatestamp ?? DateTimeOffset.MaxValue) > snapshot.SnapshotDatestamp);
        }
    }
}