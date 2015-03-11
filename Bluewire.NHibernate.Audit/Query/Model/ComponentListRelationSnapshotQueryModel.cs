using System;
using System.Collections.Generic;
using System.Linq;
using Bluewire.NHibernate.Audit.Query.Internal;
using Bluewire.NHibernate.Audit.Support;

namespace Bluewire.NHibernate.Audit.Query.Model
{
    public class ComponentListRelationSnapshotQueryModel<TEntity, TEntityKey, TValue> : IListRelationSnapshotQuery<TEntity, TEntityKey, TValue, TValue>
        where TEntity : IEntityAuditHistory<TEntityKey>
    {
        private readonly ISnapshotContext context;

        public ComponentListRelationSnapshotQueryModel(ISnapshotContext context)
        {
            this.context = context;
        }

        public ICollectionSnapshotQuery<TEntity, IList<TValue>> Using<TRelation>() where TRelation : KeyedRelationAuditHistoryEntry<TEntityKey, int, TValue>
        {
            return new ListSnapshotQuery<TRelation>(context);
        }

        class ListSnapshotQuery<TRelation> : ICollectionSnapshotQuery<TEntity, IList<TValue>>
            where TRelation : KeyedRelationAuditHistoryEntry<TEntityKey, int, TValue>
        {
            private readonly ISnapshotContext context;

            public ListSnapshotQuery(ISnapshotContext context)
            {
                this.context = context;
            }

            public IEntityCollectionMap<TEntity, IList<TValue>> Fetch(ICollection<TEntity> owners)
            {
                var snapshot = AuditQueryHelper.GetKeyedRelationSnapshot<TRelation, TEntityKey, int, TValue>(context, owners.Select(e => e.Id).ToArray());
                return new ListSnapshot(owners, snapshot.ToLookup(r => r.OwnerId));
            }

            class ListSnapshot : IEntityCollectionMap<TEntity, IList<TValue>>
            {
                private readonly ICollection<TEntity> entities;
                private readonly ILookup<TEntityKey, TRelation> relationsSnapshot;

                public ListSnapshot(ICollection<TEntity> entities, ILookup<TEntityKey, TRelation> relationsSnapshot)
                {
                    this.entities = entities;
                    this.relationsSnapshot = relationsSnapshot;
                }

                public IList<TValue> For(TEntity entity)
                {
                    if (!entities.Contains(entity)) throw new InvalidOperationException(String.Format("GetModel did not include entity with Id {0}. No data is available.", entity.Id));
                    var listEntries = relationsSnapshot[entity.Id].ToDictionary(r => r.Key, r => r.Value);
                    return CollectionHelpers.RehydrateListWithPossibleGaps(listEntries);
                }

            }
        }
    }
}