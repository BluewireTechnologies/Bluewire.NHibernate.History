using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Event;

namespace Bluewire.NHibernate.Audit.Listeners.Collectors
{
    public class SetDeletionCollector : DeletionCollector
    {
        public SetDeletionCollector(CollectionEntry collectionEntry) : base(collectionEntry)
        {
            if (Persister.HasIndex) throw new ArgumentException(String.Format("This is a keyed collection: {0}", Persister.Role));
        }

        readonly List<object> deletions = new List<object>();

        public override void Delete(IPersistentCollection collection, object entry, int index)
        {
            var item = collection.GetElement(entry);
            deletions.Add(item);
        }

        public override void Delete(IPersistentCollection collection, object key)
        {
            deletions.Add(key);
        }

        public bool IsEmpty { get { return !deletions.Any(); } }

        public IEnumerable<object> Enumerate()
        {
            return deletions;
        }

        public override void Apply(IEventSource session, ValueCollectionAuditTasks task)
        {
            task.ExecuteSetDeletion(session, this);
        }
    }
}