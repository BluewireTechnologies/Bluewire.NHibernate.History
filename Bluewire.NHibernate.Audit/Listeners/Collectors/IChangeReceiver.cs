using NHibernate.Collection;
using NHibernate.Persister.Collection;

namespace Bluewire.NHibernate.Audit.Listeners.Collectors
{
    public interface IChangeReceiver
    {
        ICollectionPersister Persister { get; }
        void Delete(IPersistentCollection collection, object entry, int index);
        void Delete(IPersistentCollection collection, object key);
        void Insert(IPersistentCollection collection, object entry, int index);
        void Update(IPersistentCollection collection, object entry, int index);
    }
}