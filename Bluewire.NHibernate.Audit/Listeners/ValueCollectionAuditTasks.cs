using System;
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

            //Debug.Assert(!collector.Persister.IsOneToMany);

            var innerSession = session.GetSession(EntityMode.Poco);
            foreach (var insertion in collector.Enumerate())
            {
                var entry = model.GenerateRelationAuditEntry(createModel, insertion, session, collector.Persister);
                entry.OwnerId = collector.OwnerKey;
                entry.StartDatestamp = sessionAuditInfo.OperationDatestamp;
                innerSession.Save(entry);
            }
            innerSession.Flush();
        }

        public void ExecuteKeyedInsertion(IEventSource session, KeyedInsertionCollector collector)
        {
            var sessionAuditInfo = GetCurrentSessionInfo(session);
            var createModel = GetRelationModel(collector.Persister);

            //Debug.Assert(!collector.Persister.IsOneToMany);

            var innerSession = session.GetSession(EntityMode.Poco);
            foreach (var insertion in collector.Enumerate())
            {
                var entry = (IKeyedRelationAuditHistory)model.GenerateRelationAuditEntry(createModel, insertion.Value, session, collector.Persister);
                entry.OwnerId = collector.OwnerKey;
                entry.Key = insertion.Key;
                entry.StartDatestamp = sessionAuditInfo.OperationDatestamp;
                innerSession.Save(entry);
            }
            innerSession.Flush();
        }

        public void ExecuteSetDeletion(IEventSource session, SetDeletionCollector collector)
        {
            var sessionAuditInfo = GetCurrentSessionInfo(session);
            var deleteModel = GetRelationModel(collector.Persister);
            if (collector.Persister.HasIndex) throw new ArgumentException(String.Format("This is a keyed collection: {0}", collector.Persister.Role));

            //Debug.Assert(!collector.Persister.IsOneToMany);

            var auditMapping = model.GetAuditClassMapping(deleteModel.AuditEntryType);
            var auditDelete = new AuditSetDeleteCommand(session.Factory, deleteModel, auditMapping);

            foreach (var deletion in collector.Enumerate())
            {
                var entry = model.GenerateRelationAuditEntry(deleteModel, deletion, session, collector.Persister);
                var expectation = Expectations.AppropriateExpectation(ExecuteUpdateResultCheckStyle.Count);
                var cmd = session.Batcher.PrepareBatchCommand(auditDelete.Command.CommandType, auditDelete.Command.Text, auditDelete.Command.ParameterTypes);
                auditDelete.PopulateCommand(session, cmd, collector.OwnerKey, entry, sessionAuditInfo.OperationDatestamp);
                session.Batcher.AddToBatch(expectation);
            }
        }

        public void ExecuteKeyedDeletion(IEventSource session, KeyedDeletionCollector collector)
        {
            if (!collector.Persister.HasIndex) throw new ArgumentException(String.Format("Not a keyed collection: {0}", collector.Persister.Role));
            var sessionAuditInfo = GetCurrentSessionInfo(session);
            var deleteModel = GetRelationModel(collector.Persister);

            //Debug.Assert(!collector.Persister.IsOneToMany);
            var auditMapping = model.GetAuditClassMapping(deleteModel.AuditEntryType);
            var auditDelete = new AuditKeyedDeleteCommand(session.Factory, auditMapping);

            foreach (var deletion in collector.Enumerate())
            {
                var expectation = Expectations.AppropriateExpectation(ExecuteUpdateResultCheckStyle.Count);
                var cmd = session.Batcher.PrepareBatchCommand(auditDelete.Command.CommandType, auditDelete.Command.Text, auditDelete.Command.ParameterTypes);
                auditDelete.PopulateCommand(session, cmd, collector.OwnerKey, deletion, sessionAuditInfo.OperationDatestamp);
                session.Batcher.AddToBatch(expectation);
            }
        }

        private SessionAuditInfo GetCurrentSessionInfo(IEventSource session)
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