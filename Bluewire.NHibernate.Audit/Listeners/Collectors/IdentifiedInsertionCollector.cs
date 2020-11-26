using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Id;

namespace Bluewire.NHibernate.Audit.Listeners.Collectors
{
    public class IdentifiedInsertionCollector : InsertionCollector
    {
        public IdentifiedInsertionCollector(CollectionEntry collectionEntry) : base(collectionEntry)
        {
            if (Persister.IdentifierGenerator == null) throw new ArgumentException(String.Format("Not an identified collection: {0}", Persister.Role));
            if (Persister.IdentifierGenerator is IPostInsertIdentifierGenerator) throw new NotSupportedException(String.Format("Cannot audit an IdBag with post-insert identifier generator {0}: {1}", Persister.IdentifierGenerator.GetType(), Persister.Role));
        }

        public override void Prepare(IPersistentCollection collection)
        {
            collection.PreInsert(Persister);
        }

        readonly List<KeyValuePair<object, object>> insertions = new List<KeyValuePair<object, object>>();

        public override void Insert(IPersistentCollection collection, object entry, int index)
        {
            var key = collection.GetIdentifier(entry, index);
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
            task.ExecuteIdentifiedInsertion(session, this);
        }
    }
}
