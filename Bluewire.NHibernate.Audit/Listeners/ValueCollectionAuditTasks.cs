using System;
using System.Diagnostics;
using System.Linq;
using Bluewire.NHibernate.Audit.Listeners.Collectors;
using Bluewire.NHibernate.Audit.Meta;
using Bluewire.NHibernate.Audit.Model;
using Bluewire.NHibernate.Audit.Runtime;
using Bluewire.NHibernate.Audit.Support;
using NHibernate;
using NHibernate.AdoNet;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Mapping;
using NHibernate.Persister.Collection;

namespace Bluewire.NHibernate.Audit.Listeners
{
    public class ValueCollectionAuditTasks
    {
        private readonly SessionsAuditInfo sessionsAuditInfo;
        private readonly AuditModel model;

        public ValueCollectionAuditTasks(SessionsAuditInfo sessionsAuditInfo, AuditModel model)
        {
            this.sessionsAuditInfo = sessionsAuditInfo;
            this.model = model;
        }

        public void ExecuteSetInsertion(IEventSource session, SetInsertionCollector collector)
        {
            var sessionAuditInfo = GetCurrentSessionInfo(session);
            var createModel = GetRelationModel(collector.Persister);

            Debug.Assert(!collector.Persister.IsOneToMany);

            foreach (var insertion in collector.Enumerate())
            {
                var entry = model.GenerateRelationAuditEntry(createModel, insertion, session, collector.Persister);
                entry.OwnerId = collector.OwnerKey;
                entry.StartDatestamp = sessionAuditInfo.OperationDatestamp;
                sessionAuditInfo.CurrentModel.QueueInsert(entry);
            }
        }

        public void ExecuteKeyedInsertion(IEventSource session, KeyedInsertionCollector collector)
        {
            var sessionAuditInfo = GetCurrentSessionInfo(session);
            var createModel = GetRelationModel(collector.Persister);

            Debug.Assert(!collector.Persister.IsOneToMany);

            foreach (var insertion in collector.Enumerate())
            {
                var entry = (IKeyedRelationAuditHistory)model.GenerateRelationAuditEntry(createModel, insertion.Value, session, collector.Persister);
                entry.OwnerId = collector.OwnerKey;
                entry.Key = insertion.Key;
                entry.StartDatestamp = sessionAuditInfo.OperationDatestamp;
                sessionAuditInfo.CurrentModel.QueueInsert(entry);
            }
        }

        public void ExecuteSetDeletion(IEventSource session, DeletionCollector collector)
        {
            var sessionAuditInfo = GetCurrentSessionInfo(session);
            var deleteModel = GetRelationModel(collector.Persister);
            if (collector.Persister.HasIndex) throw new ArgumentException(String.Format("This is a keyed collection: {0}", collector.Persister.Role));

            Debug.Assert(!collector.Persister.IsOneToMany);

            var auditMapping = model.GetAuditClassMapping(deleteModel.AuditEntryType);
            var auditDelete = new AuditSetDeleteCommand(session.Factory, deleteModel, auditMapping);

            foreach (var deletion in collector.Enumerate())
            {
                var entry = model.GenerateRelationAuditEntry(deleteModel, deletion, session, collector.Persister);
                sessionAuditInfo.CurrentModel.QueueWork(new  AuditDeleteCommandWorkItem<AuditSetDeleteCommand>(auditDelete, collector.OwnerKey, entry));
            }
        }

        public void ExecuteKeyedDeletion(IEventSource session, DeletionCollector collector)
        {
            if (!collector.Persister.HasIndex) throw new ArgumentException(String.Format("Not a keyed collection: {0}", collector.Persister.Role));
            var sessionAuditInfo = GetCurrentSessionInfo(session);
            var deleteModel = GetRelationModel(collector.Persister);

            Debug.Assert(!collector.Persister.IsOneToMany);
            var auditMapping = model.GetAuditClassMapping(deleteModel.AuditEntryType);
            var auditDelete = new AuditKeyedDeleteCommand(session.Factory, auditMapping);

            foreach (var deletion in collector.Enumerate())
            {
                sessionAuditInfo.CurrentModel.QueueWork(new AuditDeleteCommandWorkItem<AuditKeyedDeleteCommand>(auditDelete, collector.OwnerKey, deletion));
            }
        }

        private ISessionSnapshot GetCurrentSessionInfo(IEventSource session)
        {
            var sessionAuditInfo = sessionsAuditInfo.Lookup(session);
            sessionAuditInfo.AssertIsFlushing();
            return sessionAuditInfo;
        }

        private IAuditableRelationModel GetRelationModel(ICollectionPersister persister)
        {
            IAuditableRelationModel relationModel;
            if (!model.TryGetModelForPersister(persister, out relationModel))
                throw new ArgumentException(String.Format("No audit model defined for {0}", persister.Role));
            return relationModel;
        }

        class AuditDeleteCommandWorkItem<T> : IWorkItem where T : AuditDeleteCommandBase
        {
            private readonly T commandPrototype;
            private readonly object ownerKey;
            private readonly object deletion;

            public AuditDeleteCommandWorkItem(T commandPrototype, object ownerKey, object deletion)
            {
                this.commandPrototype = commandPrototype;
                this.ownerKey = ownerKey;
                this.deletion = deletion;
            }

            public void Execute(ISessionImplementor session, ISessionSnapshot snapshot)
            {
                var expectation = Expectations.AppropriateExpectation(ExecuteUpdateResultCheckStyle.Count);
                var cmd = session.Batcher.PrepareBatchCommand(commandPrototype.Command.CommandType, commandPrototype.Command.Text, commandPrototype.Command.ParameterTypes);
                commandPrototype.PopulateCommand(session, cmd, ownerKey, deletion, snapshot.OperationDatestamp);
                session.Batcher.AddToBatch(expectation);
            }
        }

        class AuditSetDeleteCommand : AuditDeleteCommandBase
        {
            private readonly Property valueProperty;
            private readonly Type entryType;

            public AuditSetDeleteCommand(ISessionFactoryImplementor factory, IAuditableRelationModel relationModel, PersistentClass auditMapping)
                : base(factory, auditMapping)
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

        class AuditKeyedDeleteCommand : AuditDeleteCommandBase
        {
            private readonly Property indexProperty;

            public AuditKeyedDeleteCommand(ISessionFactoryImplementor factory, PersistentClass auditMapping)
                : base(factory, auditMapping)
            {
                indexProperty = auditMapping.PropertyClosureIterator.Single(n => n.Name == "Key");
                AddPredicateProperty(indexProperty);
            }

            protected override void AddParameters(CommandParameteriser parameters, object deletion)
            {
                parameters.Set(indexProperty, deletion);
            }
        }
    }
}