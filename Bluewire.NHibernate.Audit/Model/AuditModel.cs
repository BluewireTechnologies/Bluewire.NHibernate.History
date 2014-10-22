using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NHibernate;
using NHibernate.Persister.Entity;

namespace Bluewire.NHibernate.Audit.Model
{
    public class AuditModel
    {
        private readonly IAuditEntryFactory auditEntryFactory;
        private Dictionary<Type, IAuditableEntityModel> entityModels;

        public AuditModel(IAuditEntryFactory auditEntryFactory, IEnumerable<IAuditableEntityModel> entityModels)
        {
            this.auditEntryFactory = auditEntryFactory;
            this.entityModels = entityModels.ToDictionary(m => m.EntityType);
        }

        public bool IsAuditable(Type entityType)
        {
            return entityModels.ContainsKey(entityType);
        }

        public IAuditHistory GenerateAuditEntry(IAuditableEntityModel entityModel, object entity)
        {
            var auditEntry = auditEntryFactory.Create(entity, entityModel.EntityType, entityModel.AuditEntryType);
            Debug.Assert(GetModelForEntityType(entityModel.EntityType).AuditEntryType.IsInstanceOfType(auditEntry));
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
    }
}