using System;
using System.Collections.Generic;
using System.Linq;
using Bluewire.NHibernate.Audit.Query.Internal;
using Bluewire.NHibernate.Audit.Support;

namespace Bluewire.NHibernate.Audit.Query.Model
{
    public class EntityListRelationSnapshotQueryModel<TEntity, TEntityKey, TRelatedEntity, TRelatedEntityKey>
        : EntitySnapshotQueryModel<TRelatedEntity, TRelatedEntityKey>, IListRelationSnapshotQuery<TEntity, TEntityKey, TRelatedEntity, TRelatedEntityKey>
        where TEntity : IEntityAuditHistory<TEntityKey>
        where TRelatedEntity : IEntityAuditHistory<TRelatedEntityKey>
    {
        private readonly ISnapshotContext context;

        public EntityListRelationSnapshotQueryModel(ISnapshotContext context) : base(context)
        {
            this.context = context;
        }

        public ICollectionSnapshotQuery<TEntity, IList<TRelatedEntity>> Using<TRelation>() where TRelation : KeyedRelationAuditHistoryEntry<TEntityKey, int, TRelatedEntityKey>
        {
            return new EntityListSnapshotQuery<TRelation>(context);
        }

        class EntityListSnapshotQuery<TRelation> : ICollectionSnapshotQuery<TEntity, IList<TRelatedEntity>>
            where TRelation : KeyedRelationAuditHistoryEntry<TEntityKey, int, TRelatedEntityKey>
        {
            private readonly ISnapshotContext context;

            public EntityListSnapshotQuery(ISnapshotContext context)
            {
                this.context = context;
            }

            public IEntityCollectionMap<TEntity, IList<TRelatedEntity>> Fetch(ICollection<TEntity> owners)
            {
                var snapshot = AuditQueryHelper.GetKeyedRelationSnapshot<TRelation, TEntityKey, int, TRelatedEntityKey>(context, owners.Select(e => e.Id).ToArray()).ToList();
                var relatedEntityIds = snapshot.Select(s => s.Value).Distinct().ToArray();
                var relatedEntities = context.GetModel<TRelatedEntity, TRelatedEntityKey>().GetMany(relatedEntityIds);
                return new EntityListSnapshot(owners, snapshot.ToLookup(r => r.OwnerId), relatedEntities);
            }

            class EntityListSnapshot : IEntityCollectionMap<TEntity, IList<TRelatedEntity>>
            {
                private readonly ICollection<TEntity> entities;
                private readonly ILookup<TEntityKey, TRelation> relationsSnapshot;
                private readonly ILookup<TRelatedEntityKey, TRelatedEntity> relatedEntities;

                public EntityListSnapshot(ICollection<TEntity> entities, ILookup<TEntityKey, TRelation> relationsSnapshot, ILookup<TRelatedEntityKey, TRelatedEntity> relatedEntities)
                {
                    this.entities = entities;
                    this.relationsSnapshot = relationsSnapshot;
                    this.relatedEntities = relatedEntities;
                }

                public IList<TRelatedEntity> For(TEntity entity)
                {
                    if (!entities.Contains(entity)) throw new InvalidOperationException(String.Format("GetModel did not include entity with Id {0}. No data is available.", entity.Id));
                    var listEntries = relationsSnapshot[entity.Id].ToDictionary(r => r.Key, r => relatedEntities[r.Value].SingleOrDefault());
                    return CollectionHelpers.RehydrateListWithPossibleGaps(listEntries);
                }
            }
        }
    }
}
