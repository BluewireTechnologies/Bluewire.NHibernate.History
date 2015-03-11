using System;
using Bluewire.NHibernate.Audit.Query.Model;
using NHibernate;

namespace Bluewire.NHibernate.Audit.Query
{
    public static class AuditQueryExtensions
    {
        public static ISnapshotContext At(this ISession session, DateTimeOffset snapshotDatestamp)
        {
            return new SessionSnapshotContext(session, snapshotDatestamp);
        }

        public static EntitySnapshotQueryModel<TEntity, TEntityKey> GetModel<TEntity, TEntityKey>(this ISnapshotContext context) where TEntity : IEntityAuditHistory<TEntityKey>
        {
            return new EntitySnapshotQueryModel<TEntity, TEntityKey>(context);
        }

        public static TCollection Fetch<TEntity, TCollection>(this ICollectionSnapshotQuery<TEntity, TCollection> query, TEntity owner)
        {
            return query.Fetch(new []{ owner }).For(owner);
        }
    }
}