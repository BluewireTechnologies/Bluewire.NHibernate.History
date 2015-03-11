using System;
using System.Collections.Generic;
using System.Linq;
using Bluewire.NHibernate.Audit.Query.Internal;

namespace Bluewire.NHibernate.Audit.Query.Model
{
    public class ComponentSetRelationSnapshotQueryModel<TEntity, TEntityKey, TValue> : ISetRelationSnapshotQuery<TEntity, TEntityKey, TValue, TValue>
        where TEntity : IEntityAuditHistory<TEntityKey>
    {
        private readonly ISnapshotContext context;

        public ComponentSetRelationSnapshotQueryModel(ISnapshotContext context)
        {
            this.context = context;
        }

        public ICollectionSnapshotQuery<TEntity, ICollection<TValue>> Using<TRelation>() where TRelation : SetRelationAuditHistoryEntry<TEntityKey, TValue>
        {
            return new SetSnapshotQuery<TRelation>(context);
        }

        class SetSnapshotQuery<TRelation> : ICollectionSnapshotQuery<TEntity, ICollection<TValue>>
            where TRelation : SetRelationAuditHistoryEntry<TEntityKey, TValue>
        {
            private readonly ISnapshotContext context;

            public SetSnapshotQuery(ISnapshotContext context)
            {
                this.context = context;
            }

            public IEntityCollectionMap<TEntity, ICollection<TValue>> Fetch(ICollection<TEntity> owners)
            {
                var snapshot = AuditQueryHelper.GetSetRelationSnapshot<TRelation, TEntityKey, TValue>(context, owners.Select(e => e.Id).ToArray());
                return new SetSnapshot(owners, snapshot.ToLookup(r => r.OwnerId));
            }

            class SetSnapshot : IEntityCollectionMap<TEntity, ICollection<TValue>>
            {
                private readonly ICollection<TEntity> entities;
                private readonly ILookup<TEntityKey, TRelation> relationsSnapshot;

                public SetSnapshot(ICollection<TEntity> entities, ILookup<TEntityKey, TRelation> relationsSnapshot)
                {
                    this.entities = entities;
                    this.relationsSnapshot = relationsSnapshot;
                }

                public ICollection<TValue> For(TEntity entity)
                {
                    if (!entities.Contains(entity)) throw new InvalidOperationException(String.Format("GetModel did not include entity with Id {0}. No data is available.", entity.Id));
                    return relationsSnapshot[entity.Id].Select(r => r.Value).ToList();
                }

            }
        }

    }
}