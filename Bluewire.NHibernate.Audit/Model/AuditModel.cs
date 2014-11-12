﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NHibernate;
using NHibernate.Mapping;
using NHibernate.Persister.Collection;
using NHibernate.Persister.Entity;

namespace Bluewire.NHibernate.Audit.Model
{
    public class AuditModel
    {
        private readonly IAuditEntryFactory auditEntryFactory;
        private readonly Dictionary<Type, IAuditableEntityModel> entityModels;
        private readonly Dictionary<string, IAuditableRelationModel> relationModels;
        private readonly Dictionary<Type, PersistentClass> auditEntryMappings;

        public AuditModel(IAuditEntryFactory auditEntryFactory, IEnumerable<IAuditableEntityModel> entityModels, IEnumerable<IAuditableRelationModel> relationModels, IEnumerable<PersistentClass> auditEntryMappings)
        {
            this.auditEntryFactory = auditEntryFactory;
            this.entityModels = entityModels.ToDictionary(m => m.EntityType);
            this.relationModels = relationModels.ToDictionary(m => m.CollectionRole);
            this.auditEntryMappings = auditEntryMappings.ToDictionary(m => m.MappedClass);
        }

        public bool IsAuditable(Type entityType)
        {
            return entityModels.ContainsKey(entityType);
        }

        public IAuditHistory GenerateAuditEntry(IAuditableEntityModel entityModel, object entity)
        {
            var auditEntry = auditEntryFactory.Create(entity, entityModel.EntityType, entityModel.AuditEntryType);
            Debug.Assert(entityModel.AuditEntryType.IsInstanceOfType(auditEntry));
            return auditEntry;
        }

        public IAuditableEntityModel GetModelForEntityType(Type entityType)
        {
            return entityModels[entityType];
        }

        public bool TryGetModelForPersister(IEntityPersister persister, out IAuditableEntityModel model)
        {
            var entityType = persister.GetMappedClass(EntityMode.Poco);
            return entityModels.TryGetValue(entityType, out model);
        }

        public bool TryGetModelForPersister(ICollectionPersister persister, out IAuditableRelationModel model)
        {
            return relationModels.TryGetValue(persister.Role, out model);
        }

        public PersistentClass GetAuditClassMapping(Type auditEntryType)
        {
            return auditEntryMappings[auditEntryType];
        }
    }
}