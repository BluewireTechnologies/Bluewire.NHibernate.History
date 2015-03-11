using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Event;

namespace Bluewire.NHibernate.Audit.Listeners.Collectors
{
    public class KeyedDeletionCollector : DeletionCollector
    {
        public KeyedDeletionCollector(CollectionEntry collectionEntry)
            : base(collectionEntry)
        {
            if (!Persister.HasIndex) throw new ArgumentException(String.Format("Not a keyed collection: {0}", Persister.Role));
        }

        readonly List<object> deletions = new List<object>();

        public override void Delete(IPersistentCollection collection, object entry, int index)
        {
            var key = collection.GetIndex(entry, index, Persister);
            deletions.Add(key);
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
            task.ExecuteKeyedDeletion(session, this);
        }
    }
}