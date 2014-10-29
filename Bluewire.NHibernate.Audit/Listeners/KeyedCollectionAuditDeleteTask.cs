using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Bluewire.NHibernate.Audit.Model;
using NHibernate.AdoNet;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Mapping;
using NHibernate.Persister.Collection;
using NHibernate.SqlCommand;

namespace Bluewire.NHibernate.Audit.Listeners
{
    class KeyedCollectionAuditDeleteTask : ICollectionAuditDeleteTask
    {
        public ICollectionPersister Persister { get; private set; }
        private readonly IPersistentCollection collection;
        private readonly SessionAuditInfo sessionAuditInfo;
        private readonly AuditModel model;
        private readonly CollectionEntry collectionEntry;
        
        public KeyedCollectionAuditDeleteTask(CollectionEntry collectionEntry, IPersistentCollection collection, SessionAuditInfo sessionAuditInfo, AuditModel model)
        {
            sessionAuditInfo.AssertIsFlushing();

            this.collectionEntry = collectionEntry;
            this.collection = collection;
            this.sessionAuditInfo = sessionAuditInfo;
            this.model = model;

            Persister = collectionEntry.LoadedPersister;
            if (Persister == null) throw new ArgumentException("No LoadedPersister for collection.", "collectionEntry");
            if (!Persister.HasIndex) throw new ArgumentException(String.Format("Not a keyed collection: {0}", Persister.Role));
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
            IAuditableRelationModel deleteModel;
            if (model.TryGetModelForPersister(Persister, out deleteModel))
            {
                var auditMapping = model.GetAuditClassMapping(deleteModel.AuditEntryType);
                var auditDelete = new AuditDeleteCommand(session.Factory, deleteModel, auditMapping);

                foreach (var deletion in deletions)
                {
                    var expectation = Expectations.AppropriateExpectation(ExecuteUpdateResultCheckStyle.Count);
                    var cmd = session.Batcher.PrepareBatchCommand(auditDelete.Command.CommandType, auditDelete.Command.Text, auditDelete.Command.ParameterTypes);
                    auditDelete.PopulateCommand(session, cmd, collectionEntry.LoadedKey, deletion, sessionAuditInfo.OperationDatestamp);
                    session.Batcher.AddToBatch(expectation);
                }
            }
        }

        class AuditDeleteCommand
        {
            private Property keyProperty;
            private Property indexProperty;
            private Property endDateProperty;

            public AuditDeleteCommand(ISessionFactoryImplementor factory, IAuditableRelationModel relationModel, PersistentClass auditMapping)
            {
                var sqlUpdateBuilder = new SqlUpdateBuilder(factory.Dialect, factory);
                    
                keyProperty = auditMapping.PropertyClosureIterator.Single(n => n.Name == relationModel.OwnerKeyPropertyName);
                indexProperty = auditMapping.PropertyClosureIterator.Single(n => n.Name == relationModel.KeyPropertyName);
                endDateProperty = auditMapping.PropertyClosureIterator.Single(n => n.Name == "EndDatestamp");

                sqlUpdateBuilder.SetTableName(auditMapping.Table.GetQualifiedName(factory.Dialect, factory.Settings.DefaultCatalogName, factory.Settings.DefaultSchemaName));

                sqlUpdateBuilder
                    .AddColumns(ColumnNames(factory, endDateProperty.ColumnIterator), endDateProperty.Type)
                    .AddWhereFragment(ColumnNames(factory, keyProperty.ColumnIterator), keyProperty.Type, " = ")
                    .AddWhereFragment(ColumnNames(factory, indexProperty.ColumnIterator), indexProperty.Type, " = ")
                    .AddWhereFragment(ColumnNames(factory, endDateProperty.ColumnIterator).Single() + " is null");


                Command = sqlUpdateBuilder.ToSqlCommandInfo();
            }

            private static string[] ColumnNames(ISessionFactoryImplementor factory, IEnumerable<ISelectable> columnIterator)
            {
                return columnIterator.Select(k => k.GetText(factory.Dialect)).ToArray();
            }

            public SqlCommandInfo Command { get; private set; }

            public void PopulateCommand(ISessionImplementor session, IDbCommand cmd, object key, object index, DateTimeOffset deletionDatestamp)
            {
                endDateProperty.Type.NullSafeSet(cmd, deletionDatestamp, 0, session);
                keyProperty.Type.NullSafeSet(cmd, key, 1, session);
                indexProperty.Type.NullSafeSet(cmd, index, 2, session);
            }
        }
    }
}