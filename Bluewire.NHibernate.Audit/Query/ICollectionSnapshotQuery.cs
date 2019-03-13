using System.Collections.Generic;

namespace Bluewire.NHibernate.Audit.Query
{
    public interface ICollectionSnapshotQuery<TEntity, TCollection>
    {
        IEntityCollectionMap<TEntity, TCollection> Fetch(ICollection<TEntity> owners);
    }
}
