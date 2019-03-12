using System;
using System.Collections.Generic;
using System.Linq;
using Bluewire.NHibernate.Audit.Query.Internal;

namespace Bluewire.NHibernate.Audit.Query.Model
{
    public class ComponentMapRelationSnapshotQueryModel<TEntity, TEntityKey, TCollectionKey, TValue> : IMapRelationSnapshotQuery<TEntity, TEntityKey, TCollectionKey, TValue, TValue>
        where TEntity : IEntityAuditHistory<TEntityKey>
    {
        private readonly ISnapshotContext context;

        public ComponentMapRelationSnapshotQueryModel(ISnapshotContext context)
        {
            this.context = context;
        }

        public ICollectionSnapshotQuery<TEntity, IDictionary<TCollectionKey, TValue>> Using<TRelation>()
            where TRelation : KeyedRelationAuditHistoryEntry<TEntityKey, TCollectionKey, TValue>
        {
            return new MapSnapshotQuery<TRelation>(context);
        }

        class MapSnapshotQuery<TRelation> : ICollectionSnapshotQuery<TEntity, IDictionary<TCollectionKey, TValue>>
            where TRelation : KeyedRelationAuditHistoryEntry<TEntityKey, TCollectionKey, TValue>
        {
            private readonly ISnapshotContext context;

            public MapSnapshotQuery(ISnapshotContext context)
            {
                this.context = context;
            }

            public IEntityCollectionMap<TEntity, IDictionary<TCollectionKey, TValue>> Fetch(ICollection<TEntity> owners)
            {
                var snapshot = AuditQueryHelper.GetKeyedRelationSnapshot<TRelation, TEntityKey, TCollectionKey, TValue>(context, owners.Select(e => e.Id).ToArray());
                return new MapSnapshot(owners, snapshot.ToLookup(r => r.OwnerId));
            }

            class MapSnapshot : IEntityCollectionMap<TEntity, IDictionary<TCollectionKey, TValue>>
            {
                private readonly ICollection<TEntity> entities;
                private readonly ILookup<TEntityKey, TRelation> relationsSnapshot;

                public MapSnapshot(ICollection<TEntity> entities, ILookup<TEntityKey, TRelation> relationsSnapshot)
                {
                    this.entities = entities;
                    this.relationsSnapshot = relationsSnapshot;
                }

                public IDictionary<TCollectionKey, TValue> For(TEntity entity)
                {
                    if (!entities.Contains(entity)) throw new InvalidOperationException(String.Format("GetModel did not include entity with Id {0}. No data is available.", entity.Id));
                    return relationsSnapshot[entity.Id].ToDictionary(r => r.Key, r => r.Value);
                }
            }
        }
    }
}
