namespace Bluewire.NHibernate.Audit.Query
{
    public interface IEntityCollectionMap<in TEntity, out TCollection>
    {
        TCollection For(TEntity entity);
    }
}
