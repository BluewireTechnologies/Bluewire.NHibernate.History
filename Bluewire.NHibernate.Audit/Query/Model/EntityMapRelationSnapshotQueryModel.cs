using System;
using System.Collections.Generic;
using System.Linq;
using Bluewire.NHibernate.Audit.Query.Internal;

namespace Bluewire.NHibernate.Audit.Query.Model
{
    public class EntityMapRelationSnapshotQueryModel<TEntity, TEntityKey, TCollectionKey, TRelatedEntity, TRelatedEntityKey>
        : EntitySnapshotQueryModel<TRelatedEntity, TRelatedEntityKey>, IMapRelationSnapshotQuery<TEntity, TEntityKey, TCollectionKey, TRelatedEntity, TRelatedEntityKey>
        where TEntity : IEntityAuditHistory<TEntityKey>
        where TRelatedEntity : IEntityAuditHistory<TRelatedEntityKey>
    {
        private readonly ISnapshotContext context;

        public EntityMapRelationSnapshotQueryModel(ISnapshotContext context) : base(context)
        {
            this.context = context;
        }

        public ICollectionSnapshotQuery<TEntity, IDictionary<TCollectionKey, TRelatedEntity>> Using<TRelation>()
            where TRelation : KeyedRelationAuditHistoryEntry<TEntityKey, TCollectionKey, TRelatedEntityKey>
        {
            return new EntityMapSnapshotQuery<TRelation>(context);
        }

        class EntityMapSnapshotQuery<TRelation> : ICollectionSnapshotQuery<TEntity, IDictionary<TCollectionKey, TRelatedEntity>>
            where TRelation : KeyedRelationAuditHistoryEntry<TEntityKey, TCollectionKey, TRelatedEntityKey>
        {
            private readonly ISnapshotContext context;

            public EntityMapSnapshotQuery(ISnapshotContext context)
            {
                this.context = context;
            }

            public IEntityCollectionMap<TEntity, IDictionary<TCollectionKey, TRelatedEntity>> Fetch(ICollection<TEntity> owners)
            {
                var snapshot = AuditQueryHelper.GetKeyedRelationSnapshot<TRelation, TEntityKey, TCollectionKey, TRelatedEntityKey>(context, owners.Select(e => e.Id).ToArray()).ToList();
                var relatedEntityIds = snapshot.Select(s => s.Value).Distinct().ToArray();
                var relatedEntities = context.GetModel<TRelatedEntity, TRelatedEntityKey>().GetMany(relatedEntityIds);
                return new EntityMapSnapshot(owners, snapshot.ToLookup(r => r.OwnerId), relatedEntities);
            }


            class EntityMapSnapshot : IEntityCollectionMap<TEntity, IDictionary<TCollectionKey, TRelatedEntity>>
            {
                private readonly ICollection<TEntity> entities;
                private readonly ILookup<TEntityKey, TRelation> relationsSnapshot;
                private readonly ILookup<TRelatedEntityKey, TRelatedEntity> relatedEntities;

                public EntityMapSnapshot(ICollection<TEntity> entities, ILookup<TEntityKey, TRelation> relationsSnapshot, ILookup<TRelatedEntityKey, TRelatedEntity> relatedEntities)
                {
                    this.entities = entities;
                    this.relationsSnapshot = relationsSnapshot;
                    this.relatedEntities = relatedEntities;
                }

                public IDictionary<TCollectionKey, TRelatedEntity> For(TEntity entity)
                {
                    if (!entities.Contains(entity)) throw new InvalidOperationException(String.Format("GetModel did not include entity with Id {0}. No data is available.", entity.Id));
                    return relationsSnapshot[entity.Id].ToDictionary(r => r.Key, r => relatedEntities[r.Value].SingleOrDefault());
                }
            }
        }
    }
}
