using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Bluewire.Common.Extensions;
using Bluewire.NHibernate.Audit.Model;
using NHibernate;
using NHibernate.AdoNet;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Mapping;
using NHibernate.Persister.Collection;
using NHibernate.Persister.Entity;
using NHibernate.SqlCommand;
using NHibernate.SqlTypes;
using NHibernate.Util;

namespace Bluewire.NHibernate.Audit.Listeners
{
    public class SaveListAuditEntry : IPreCollectionRecreateEventListener, IPreCollectionUpdateEventListener, IPreCollectionRemoveEventListener
    {
        private readonly SessionsAuditInfo sessions;
        private readonly AuditModel model;

        public SaveListAuditEntry(SessionsAuditInfo sessions, AuditModel model)
        {
            this.sessions = sessions;
            this.model = model;
        }

        private IList<object> GetAllEntries(IPersistentCollection collection, ICollectionPersister persister)
        {
            return collection.Entries(persister).Cast<object>().ToList();
        }

        class CollectionAuditTask
        {
            public CollectionEntry CollectionEntry { get; private set; }
            private readonly SessionAuditInfo sessionAuditInfo;
            private readonly AuditModel model;

            public CollectionAuditTask(CollectionEntry collectionEntry, SessionAuditInfo sessionAuditInfo, AuditModel model)
            {
                sessionAuditInfo.AssertIsFlushing();

                this.CollectionEntry = collectionEntry;
                this.sessionAuditInfo = sessionAuditInfo;
                this.model = model;
            }

            List<int> deletions = new List<int>();
            List<Tuple<object, int>> insertions = new List<Tuple<object, int>>();

            public void Delete(int index)
            {
                deletions.Add(index);
            }

            public void Insert(object entry, int index)
            {
                insertions.Add(new Tuple<object, int>(entry, index));
            }

            public void Execute(IEventSource session, ICollectionPersister deletePersister, ICollectionPersister createPersister)
            {   
                IAuditableRelationModel deleteModel;
                if (model.TryGetModelForPersister(deletePersister, out deleteModel))
                {
                    var auditMapping = model.GetAuditClassMapping(deleteModel.AuditEntryType);
                    var auditDelete = new AuditDeleteCommand(session.Factory, deleteModel, auditMapping);

                    foreach (var deletion in deletions)
                    {
                        var expectation = Expectations.AppropriateExpectation(ExecuteUpdateResultCheckStyle.Count);
                        var cmd = session.Batcher.PrepareBatchCommand(auditDelete.Command.CommandType, auditDelete.Command.Text, auditDelete.Command.ParameterTypes);
                        auditDelete.PopulateCommand(session, cmd, CollectionEntry.LoadedKey, deletion, sessionAuditInfo.OperationDatestamp);
                        session.Batcher.AddToBatch(expectation);
                    }
                }
                IAuditableRelationModel createModel;
                if (model.TryGetModelForPersister(createPersister, out createModel))
                {
                    var auditMapping = model.GetAuditClassMapping(createModel.AuditEntryType);

                    var innerSession = session.GetSession(EntityMode.Poco);
                    foreach (var insertion in insertions)
                    {
                        var entry = model.GenerateRelationAuditEntry<ListElementAuditHistory>(createModel, insertion.Item1);
                        auditMapping.PropertyClosureIterator.Single(p => p.Name == createModel.OwnerKeyPropertyName).GetSetter(createModel.AuditEntryType).Set(entry, CollectionEntry.CurrentKey);
                        entry.Index = insertion.Item2;
                        entry.StartDatestamp = sessionAuditInfo.OperationDatestamp;
                        innerSession.Save(entry);
                    }
                    innerSession.Flush();
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
                    indexProperty = auditMapping.PropertyClosureIterator.Single(n => n.Name == "Index");
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

                public void PopulateCommand(ISessionImplementor session, IDbCommand cmd, object key, int index, DateTimeOffset deletionDatestamp)
                {
                    endDateProperty.Type.NullSafeSet(cmd, deletionDatestamp, 0, session);
                    keyProperty.Type.NullSafeSet(cmd, key, 1, session);
                    indexProperty.Type.NullSafeSet(cmd, index, 2, session);
                }
            }
        }

        public void OnPreUpdateCollection(PreCollectionUpdateEvent @event)
        {
            var collection = @event.Collection;
            var collectionEntry = @event.Session.PersistenceContext.GetCollectionEntry(@event.Collection);

            Debug.Assert(collectionEntry.CurrentPersister == collectionEntry.LoadedPersister);
            var creationPersister = collectionEntry.CurrentPersister;
            Debug.Assert(creationPersister.HasIndex);
            Debug.Assert(!creationPersister.IsOneToMany);

            var deletionPersister = collectionEntry.LoadedPersister;
            Debug.Assert(deletionPersister.HasIndex);
            Debug.Assert(!deletionPersister.IsOneToMany);

            var task = new CollectionAuditTask(collectionEntry, sessions.Lookup(@event.Session), model);

            var hasFilters = collectionEntry.LoadedPersister.IsAffectedByEnabledFilters(@event.Session);
            if (!collection.WasInitialized)
            {
                if (!collection.HasQueuedOperations)
                    throw new AssertionFailure("no queued adds");
            }
            else if (!hasFilters && collection.Empty)
            {
                RecordDestruction(task, collection);
            }
            else if (collection.NeedsRecreate(collectionEntry.CurrentPersister))
            {
                if (hasFilters) throw new HibernateException("cannot recreate collection while filter is enabled");
                RecordDestruction(task, collection);
                RecordCreation(task, collection);
            }
            else
            {
                var deletions = collection.GetDeletes(deletionPersister, false).Cast<int>();
                foreach (var d in deletions)
                {
                    task.Delete(d);
                }
                var index = 0;
                foreach (var entry in collection.Entries(deletionPersister))
                {
                    if (collection.NeedsUpdating(entry, index, deletionPersister.ElementType))
                    {
                        task.Delete(index);
                    }
                    ++index;
                }
                index = 0;
                foreach (var entry in collection.Entries(creationPersister))
                {
                    if (collection.NeedsUpdating(entry, index, creationPersister.ElementType) ||
                        collection.NeedsInserting(entry, index, creationPersister.ElementType))
                    {
                        task.Insert(entry, index);
                    }
                    ++index;
                }
            }
            task.Execute(@event.Session, collectionEntry.LoadedPersister, collectionEntry.CurrentPersister);
        }

        private void RecordCreation(CollectionAuditTask task, IPersistentCollection collection)
        {
            var index = 0;
            foreach (var item in GetAllEntries(collection, task.CollectionEntry.CurrentPersister))
            {
                task.Insert(item, index);
                ++index;
            }
        }

        private void RecordDestruction(CollectionAuditTask task, IPersistentCollection collection)
        {
            if (task.CollectionEntry.LoadedPersister == null) return;
            var emptySnapshot = task.CollectionEntry.IsSnapshotEmpty(collection);
            if (emptySnapshot) return;

            var index = 0;
            foreach (var item in GetAllEntries(collection, task.CollectionEntry.LoadedPersister))
            {
                task.Delete(index);
                ++index;
            }
        }

        public void OnPreRemoveCollection(PreCollectionRemoveEvent @event)
        {
            var collectionEntry = @event.Session.PersistenceContext.GetCollectionEntry(@event.Collection);
            var task = new CollectionAuditTask(collectionEntry, sessions.Lookup(@event.Session), model);

            RecordDestruction(task, @event.Collection);
            task.Execute(@event.Session, collectionEntry.LoadedPersister, collectionEntry.CurrentPersister);
        }

        public void OnPreRecreateCollection(PreCollectionRecreateEvent @event)
        {
            var collectionEntry = @event.Session.PersistenceContext.GetCollectionEntry(@event.Collection);
            var task = new CollectionAuditTask(collectionEntry, sessions.Lookup(@event.Session), model);

            RecordDestruction(task, @event.Collection);
            RecordCreation(task, @event.Collection);
            task.Execute(@event.Session, collectionEntry.LoadedPersister ?? collectionEntry.CurrentPersister, collectionEntry.CurrentPersister);
        }
    }
}
