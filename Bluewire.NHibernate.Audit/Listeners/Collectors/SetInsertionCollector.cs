using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Event;

namespace Bluewire.NHibernate.Audit.Listeners.Collectors
{
    public class SetInsertionCollector : InsertionCollector
    {
        public SetInsertionCollector(CollectionEntry collectionEntry) : base(collectionEntry)
        {
            if (Persister.HasIndex) throw new ArgumentException(String.Format("This is a keyed collection: {0}", Persister.Role));
        }

        readonly List<object> insertions = new List<object>();

        public override void Insert(IPersistentCollection collection, object entry, int index)
        {
            var item = collection.GetElement(entry);
            insertions.Add(item);
        }

        public IEnumerable<object> Enumerate()
        {
            return insertions;
        }
    
        public override bool IsEmpty { get { return !insertions.Any(); } }

        public override void Apply(IEventSource session, ValueCollectionAuditTasks task)
        {
            task.ExecuteSetInsertion(session, this);
        }
    }
}