using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Id;

namespace Bluewire.NHibernate.Audit.Listeners.Collectors
{
    public class IdentifiedDeletionCollector : DeletionCollector
    {
        public IdentifiedDeletionCollector(CollectionEntry collectionEntry)
            : base(collectionEntry)
        {
            if (Persister.IdentifierGenerator == null) throw new ArgumentException(String.Format("Not an identified collection: {0}", Persister.Role));
            if (Persister.IdentifierGenerator is IPostInsertIdentifierGenerator) throw new NotSupportedException(String.Format("Cannot audit an IdBag with post-insert identifier generator {0}: {1}", Persister.IdentifierGenerator.GetType(), Persister.Role));
        }

        readonly List<object> deletions = new List<object>();

        public override void Delete(IPersistentCollection collection, object entry, int index)
        {
            var key = collection.GetIdentifier(entry, index);
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
            task.ExecuteIdentifiedDeletion(session, this);
        }
    }
}
