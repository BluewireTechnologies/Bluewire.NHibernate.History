using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Event;

namespace Bluewire.NHibernate.Audit.Listeners.Collectors
{
    public class KeyedInsertionCollector : InsertionCollector
    {
        public KeyedInsertionCollector(CollectionEntry collectionEntry) : base(collectionEntry)
        {
            if (!Persister.HasIndex) throw new ArgumentException(String.Format("Not a keyed collection: {0}", Persister.Role));
        }

        readonly List<KeyValuePair<object, object>> insertions = new List<KeyValuePair<object, object>>();

        public override void Insert(IPersistentCollection collection, object entry, int index)
        {
            var key = collection.GetIndex(entry, index, Persister);
            var item = collection.GetElement(entry);
            insertions.Add(new KeyValuePair<object, object>(key, item));
        }

        public IEnumerable<KeyValuePair<object, object>> Enumerate()
        {
            return insertions;
        }

        public override bool IsEmpty { get { return !insertions.Any(); } }

        public override void Apply(IEventSource session, ValueCollectionAuditTasks task)
        {
            task.ExecuteKeyedInsertion(session, this);
        }
    }
}
