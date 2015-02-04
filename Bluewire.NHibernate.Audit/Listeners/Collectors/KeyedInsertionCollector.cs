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
        public KeyedInsertionCollector(CollectionEntry collectionEntry, IPersistentCollection collection) : base(collectionEntry, collection)
        {
            if (!Persister.HasIndex) throw new ArgumentException(String.Format("Not a keyed collection: {0}", Persister.Role));
        }

        readonly List<Tuple<object, object>> insertions = new List<Tuple<object, object>>();
        
        protected override void Insert(object entry, int index)
        {
            var key = Collection.GetIndex(entry, index, Persister);
            var item = Collection.GetElement(entry);
            insertions.Add(new Tuple<object, object>(item, key));
        }

        public IEnumerable<Tuple<object, object>> Enumerate()
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