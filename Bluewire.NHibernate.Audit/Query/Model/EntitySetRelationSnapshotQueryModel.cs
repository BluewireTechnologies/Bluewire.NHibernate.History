using System;
using System.Collections.Generic;
using System.Linq;
using Bluewire.NHibernate.Audit.Query.Internal;

namespace Bluewire.NHibernate.Audit.Query.Model
{
    public class EntitySetRelationSnapshotQueryModel<TEntity, TEntityKey, TRelatedEntity, TRelatedEntityKey>
        : EntitySnapshotQueryModel<TRelatedEntity, TRelatedEntityKey>, ISetRelationSnapshotQuery<TEntity, TEntityKey, TRelatedEntity, TRelatedEntityKey>
        where TEntity : IEntityAuditHistory<TEntityKey>
        where TRelatedEntity : IEntityAuditHistory<TRelatedEntityKey>
    {
        private readonly ISnapshotContext context;

        public EntitySetRelationSnapshotQueryModel(ISnapshotContext context) : base(context)
        {
            this.context = context;
        }

        public ICollectionSnapshotQuery<TEntity, ICollection<TRelatedEntity>> Using<TRelation>() where TRelation : SetRelationAuditHistoryEntry<TEntityKey, TRelatedEntityKey>
        {
            return new EntitySetSnapshotQuery<TRelation>(context);
        }

        class EntitySetSnapshotQuery<TRelation> : ICollectionSnapshotQuery<TEntity, ICollection<TRelatedEntity>>
            where TRelation : SetRelationAuditHistoryEntry<TEntityKey, TRelatedEntityKey>
        {
            private readonly ISnapshotContext context;

            public EntitySetSnapshotQuery(ISnapshotContext context)
            {
                this.context = context;
            }

            public IEntityCollectionMap<TEntity, ICollection<TRelatedEntity>> Fetch(ICollection<TEntity> owners)
            {
                var snapshot = AuditQueryHelper.GetSetRelationSnapshot<TRelation, TEntityKey, TRelatedEntityKey>(context, owners.Select(e => e.Id).ToArray()).ToList();
                var relatedEntityIds = snapshot.Select(s => s.Value).Distinct().ToArray();
                var relatedEntities = context.GetModel<TRelatedEntity, TRelatedEntityKey>().GetMany(relatedEntityIds);
                return new EntitySetSnapshot(owners, snapshot.ToLookup(r => r.OwnerId), relatedEntities);
            }

            class EntitySetSnapshot : IEntityCollectionMap<TEntity, ICollection<TRelatedEntity>>
            {
                private readonly ICollection<TEntity> entities;
                private readonly ILookup<TEntityKey, TRelation> relationsSnapshot;
                private readonly ILookup<TRelatedEntityKey, TRelatedEntity> relatedEntities;

                public EntitySetSnapshot(ICollection<TEntity> entities, ILookup<TEntityKey, TRelation> relationsSnapshot, ILookup<TRelatedEntityKey, TRelatedEntity> relatedEntities)
                {
                    this.entities = entities;
                    this.relationsSnapshot = relationsSnapshot;
                    this.relatedEntities = relatedEntities;
                }

                public ICollection<TRelatedEntity> For(TEntity entity)
                {
                    if (!entities.Contains(entity)) throw new InvalidOperationException(String.Format("GetModel did not include entity with Id {0}. No data is available.", entity.Id));
                    return relationsSnapshot[entity.Id].Select(r => relatedEntities[r.Value].SingleOrDefault()).ToList();
                }
            }
        }
    }
}
