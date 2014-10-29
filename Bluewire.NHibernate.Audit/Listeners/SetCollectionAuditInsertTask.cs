﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Bluewire.NHibernate.Audit.Model;
using NHibernate;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Persister.Collection;

namespace Bluewire.NHibernate.Audit.Listeners
{
    class SetCollectionAuditInsertTask : ICollectionAuditInsertTask
    {
        public ICollectionPersister Persister { get; private set; }
        private readonly IPersistentCollection collection;
        private readonly SessionAuditInfo sessionAuditInfo;
        private readonly AuditModel model;
        private readonly CollectionEntry collectionEntry;

        public SetCollectionAuditInsertTask(CollectionEntry collectionEntry, IPersistentCollection collection, SessionAuditInfo sessionAuditInfo, AuditModel model)
        {
            sessionAuditInfo.AssertIsFlushing();

            this.collectionEntry = collectionEntry;
            this.collection = collection;
            this.sessionAuditInfo = sessionAuditInfo;
            this.model = model;

            Persister = collectionEntry.CurrentPersister;
            if (Persister == null) throw new ArgumentException("No CurrentPersister for collection.", "collectionEntry");
            if (Persister.HasIndex) throw new ArgumentException(String.Format("This is a keyed collection: {0}", Persister.Role));
            Debug.Assert(!Persister.IsOneToMany);
        }

        readonly List<object> insertions = new List<object>();

        public void InsertAll()
        {
            var index = 0;
            foreach (var item in collection.Entries(Persister))
            {
                Insert(item, index);
                ++index;
            }
        }

        public void Insert(object entry, int index)
        {
            var item = collection.GetElement(entry);
            insertions.Add(item);
        }

        public void Execute(IEventSource session)
        {
            IAuditableRelationModel createModel;
            if (model.TryGetModelForPersister(Persister, out createModel))
            {
                var auditMapping = model.GetAuditClassMapping(createModel.AuditEntryType);

                var innerSession = session.GetSession(EntityMode.Poco);
                foreach (var insertion in insertions)
                {
                    var entry = model.GenerateRelationAuditEntry<ISetRelationAuditHistory>(createModel, insertion);
                    auditMapping.PropertyClosureIterator.Single(p => p.Name == createModel.OwnerKeyPropertyName).GetSetter(createModel.AuditEntryType).Set(entry, collectionEntry.CurrentKey);
                    entry.StartDatestamp = sessionAuditInfo.OperationDatestamp;
                    innerSession.Save(entry);
                }
                innerSession.Flush();
            }
        }
    }
}