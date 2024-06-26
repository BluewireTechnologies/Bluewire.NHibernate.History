﻿using System;
using System.Data;
using System.Data.Common;
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
using NHibernate.SqlCommand;

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

        private ISession CreateChildSession(IEventSource session)
        {
            return session
                .SessionWithOptions()
                .Connection()
                .ConnectionReleaseMode()
                .FlushMode()
                .Interceptor()
                .OpenSession();
        }

        public void ExecuteSetInsertion(IEventSource session, SetInsertionCollector collector)
        {
            var sessionAuditInfo = GetCurrentSessionInfo(session);
            var createModel = GetRelationModel(collector.Persister);

            //Debug.Assert(!collector.Persister.IsOneToMany);

            using (var innerSession = CreateChildSession(session))
            {
                foreach (var insertion in collector.Enumerate())
                {
                    var entry = model.GenerateRelationAuditEntry(createModel, insertion, session, collector.Persister);
                    entry.OwnerId = collector.OwnerKey;
                    entry.StartDatestamp = sessionAuditInfo.OperationDatestamp;
                    innerSession.Save(entry);
                }
                innerSession.Flush();
            }
        }

        public void ExecuteKeyedInsertion(IEventSource session, KeyedInsertionCollector collector)
        {
            var sessionAuditInfo = GetCurrentSessionInfo(session);
            var createModel = GetRelationModel(collector.Persister);

            using (var innerSession = CreateChildSession(session))
            {
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
        }

        public void ExecuteIdentifiedInsertion(IEventSource session, IdentifiedInsertionCollector collector)
        {
            var sessionAuditInfo = GetCurrentSessionInfo(session);
            var createModel = GetRelationModel(collector.Persister);

            using (var innerSession = CreateChildSession(session))
            {
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
        }

        public void ExecuteSetDeletion(IEventSource session, SetDeletionCollector collector)
        {
            var sessionAuditInfo = GetCurrentSessionInfo(session);
            var deleteModel = GetRelationModel(collector.Persister);
            if (collector.Persister.HasIndex) throw new ArgumentException(String.Format("This is a keyed collection: {0}", collector.Persister.Role));

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

        public void ExecuteIdentifiedDeletion(IEventSource session, IdentifiedDeletionCollector collector)
        {
            if (collector.Persister.IdentifierGenerator == null) throw new ArgumentException(String.Format("Not an identified collection: {0}", collector.Persister.Role));
            var sessionAuditInfo = GetCurrentSessionInfo(session);
            var deleteModel = GetRelationModel(collector.Persister);

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
            {
                throw new ArgumentException(String.Format("No audit model defined for {0}", persister.Role));
            }
            return relationModel;
        }

        /// <summary>
        /// Generates a command of the form:
        ///     update (audit table) set endDatestamp = ? where (owner key) = ? and (entity value) = ...? and endDatestamp is null
        /// </summary>
        class AuditSetDeleteCommand
        {
            private readonly IAuditableRelationModel relationModel;
            private readonly Property owningEntityIdProperty;
            private readonly Property valueProperty;
            private readonly Property endDateProperty;

            public AuditSetDeleteCommand(ISessionFactoryImplementor factory, IAuditableRelationModel relationModel, PersistentClass auditMapping)
            {
                this.relationModel = relationModel;
                SqlUpdateBuilder = new SqlUpdateBuilder(factory.Dialect, factory);

                endDateProperty = auditMapping.PropertyClosureIterator.Single(n => n.Name == "EndDatestamp");

                SqlUpdateBuilder
                    .SetTableName(auditMapping.Table.GetQualifiedName(factory.Dialect, factory.Settings.DefaultCatalogName, factory.Settings.DefaultSchemaName))
                    .AddColumns(factory.ColumnNames(endDateProperty.ColumnIterator), endDateProperty.Type);

                owningEntityIdProperty = auditMapping.PropertyClosureIterator.Single(n => n.Name == "OwnerId");
                valueProperty = auditMapping.PropertyClosureIterator.Single(n => n.Name == "Value");

                SqlUpdateBuilder.AddWhereFragment(factory.ColumnNames(owningEntityIdProperty.ColumnIterator), owningEntityIdProperty.Type, " = ");
                SqlUpdateBuilder.AddWhereFragment(factory.ColumnNames(valueProperty.ColumnIterator), valueProperty.Type, " = ");
                SqlUpdateBuilder.AddWhereFragment(factory.ColumnNames(endDateProperty.ColumnIterator).Single() + " is null");
            }

            private SqlUpdateBuilder SqlUpdateBuilder { get; }
            public SqlCommandInfo Command => SqlUpdateBuilder.ToSqlCommandInfo();

            public void PopulateCommand(ISessionImplementor session, DbCommand cmd, object owningEntityId, object deletion, DateTimeOffset deletionDatestamp)
            {
                var parameters = new CommandParameteriser(session, cmd);
                parameters.Set(endDateProperty, deletionDatestamp);

                parameters.Set(owningEntityIdProperty, owningEntityId);
                parameters.Set(valueProperty, valueProperty.GetGetter(relationModel.AuditEntryType).Get(deletion));
            }
        }

        /// <summary>
        /// Generates a command of the form:
        ///     update (audit table) set endDatestamp = ? where (owner key) = ? and (index) = ? and endDatestamp is null
        /// </summary>
        class AuditKeyedDeleteCommand
        {
            private readonly Property owningEntityIdProperty;
            private readonly Property endDateProperty;
            private readonly Property indexProperty;

            public AuditKeyedDeleteCommand(ISessionFactoryImplementor factory, PersistentClass auditMapping)
            {
                SqlUpdateBuilder = new SqlUpdateBuilder(factory.Dialect, factory);

                endDateProperty = auditMapping.PropertyClosureIterator.Single(n => n.Name == "EndDatestamp");

                SqlUpdateBuilder
                    .SetTableName(auditMapping.Table.GetQualifiedName(factory.Dialect, factory.Settings.DefaultCatalogName, factory.Settings.DefaultSchemaName))
                    .AddColumns(factory.ColumnNames(endDateProperty.ColumnIterator), endDateProperty.Type);

                owningEntityIdProperty = auditMapping.PropertyClosureIterator.Single(n => n.Name == "OwnerId");
                indexProperty = auditMapping.PropertyClosureIterator.Single(n => n.Name == "Key");

                SqlUpdateBuilder.AddWhereFragment(factory.ColumnNames(owningEntityIdProperty.ColumnIterator), owningEntityIdProperty.Type, " = ");
                SqlUpdateBuilder.AddWhereFragment(factory.ColumnNames(indexProperty.ColumnIterator), indexProperty.Type, " = ");
                SqlUpdateBuilder.AddWhereFragment(factory.ColumnNames(endDateProperty.ColumnIterator).Single() + " is null");
            }

            private SqlUpdateBuilder SqlUpdateBuilder { get; }
            public SqlCommandInfo Command => SqlUpdateBuilder.ToSqlCommandInfo();

            public void PopulateCommand(ISessionImplementor session, DbCommand cmd, object owningEntityId, object deletion, DateTimeOffset deletionDatestamp)
            {
                var parameters = new CommandParameteriser(session, cmd);
                parameters.Set(endDateProperty, deletionDatestamp);

                parameters.Set(owningEntityIdProperty, owningEntityId);
                parameters.Set(indexProperty, deletion);
            }
        }
    }
}
