using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Bluewire.NHibernate.Audit.Model;
using Bluewire.NHibernate.Audit.Support;
using NHibernate.AdoNet;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Mapping;
using NHibernate.Persister.Collection;

namespace Bluewire.NHibernate.Audit.Listeners
{
    class SetCollectionAuditDeleteTask : ICollectionAuditDeleteTask
    {
        public ICollectionPersister Persister { get; private set; }
        private readonly IPersistentCollection collection;
        private readonly SessionAuditInfo sessionAuditInfo;
        private readonly AuditModel model;
        private readonly CollectionEntry collectionEntry;
        private readonly IAuditableRelationModel deleteModel;

        public SetCollectionAuditDeleteTask(CollectionEntry collectionEntry, IPersistentCollection collection, SessionAuditInfo sessionAuditInfo, AuditModel model)
        {
            sessionAuditInfo.AssertIsFlushing();

            this.collectionEntry = collectionEntry;
            this.collection = collection;
            this.sessionAuditInfo = sessionAuditInfo;
            this.model = model;

            Persister = collectionEntry.LoadedPersister;
            if (Persister == null) throw new ArgumentException("No LoadedPersister for collection.", "collectionEntry");
            if (Persister.HasIndex) throw new ArgumentException(String.Format("This is a keyed collection: {0}", Persister.Role));
            
            if (!model.TryGetModelForPersister(Persister, out deleteModel)) throw new ArgumentException(String.Format("No audit model defined for {0}", Persister.Role));

            Debug.Assert(!Persister.IsOneToMany);
        }

        readonly List<object> deletions = new List<object>();

        public void DeleteAll()
        {
            var emptySnapshot = collectionEntry.IsSnapshotEmpty(collection);
            if (emptySnapshot) return;

            var index = 0;
            foreach (var item in collection.Entries(Persister))
            {
                Delete(item, index);
                index++;
            }
        }

        public void Delete(object entry, int index)
        {
            var key = collection.GetIndex(entry, index, Persister);
            deletions.Add(key);
        }

        public void Delete(object key)
        {
            deletions.Add(key);
        }

        public void Execute(IEventSource session)
        {
            var auditMapping = model.GetAuditClassMapping(deleteModel.AuditEntryType);
            var auditDelete = new AuditDeleteCommand(session.Factory, deleteModel, auditMapping);

            foreach (var deletion in deletions)
            {
                var entry = model.GenerateRelationAuditEntry(deleteModel, deletion, session, Persister);
                var expectation = Expectations.AppropriateExpectation(ExecuteUpdateResultCheckStyle.Count);
                var cmd = session.Batcher.PrepareBatchCommand(auditDelete.Command.CommandType, auditDelete.Command.Text, auditDelete.Command.ParameterTypes);
                auditDelete.PopulateCommand(session, cmd, collectionEntry.LoadedKey, entry, sessionAuditInfo.OperationDatestamp);
                session.Batcher.AddToBatch(expectation);
            }
        }

        class AuditDeleteCommand : AuditDeleteCommandBase
        {
            private readonly Property valueProperty;
            private readonly Type entryType;

            public AuditDeleteCommand(ISessionFactoryImplementor factory, IAuditableRelationModel relationModel, PersistentClass auditMapping) : base(factory, auditMapping)
            {
                entryType = relationModel.AuditEntryType;

                valueProperty = auditMapping.PropertyClosureIterator.Single(n => n.Name == "Value");
                AddPredicateProperty(valueProperty);
            }

            protected override void AddParameters(CommandParameteriser parameters, object deletion)
            {
                parameters.Set(valueProperty, valueProperty.GetGetter(entryType).Get(deletion));
            }
        }
    }
}