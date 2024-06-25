using System;
using Bluewire.NHibernate.Audit.Attributes;
using Bluewire.NHibernate.Audit.Meta;
using NHibernate.Mapping;

namespace Bluewire.NHibernate.Audit.Model
{
    public class AuditEntityModelFactory
    {
        public IAuditableEntityModel CreateEntityModel(PersistentClass classMapping, AuditableEntityAttribute auditAttribute)
        {
            if (!classMapping.IsVersioned || classMapping.Version == null) throw new AuditConfigurationException(classMapping.MappedClass, "The NHibernate mapping for this type does not define a property to use for versioning.");
            ValidateCollections(classMapping);
            return CreateEntityModel(classMapping.MappedClass, auditAttribute);
        }

        private void ValidateCollections(PersistentClass classMapping)
        {
            foreach (var property in classMapping.PropertyClosureIterator)
            {
                if (!property.IsOptimisticLocked) continue;
                if (property.Type.IsCollectionType)
                {
                    var collection = property.Value as Collection;
                    if (collection?.IsInverse == true)
                    {
                        throw new AuditConfigurationException(classMapping.MappedClass, $"This type has an inverse-mapped collection '{property.Name}' which is configured to use optimistic locking. Inverse-mapped collections must specify 'OptimisticLock(false)'.");
                    }
                }
            }
        }

        public IAuditableEntityModel CreateEntityModel(Type entityType, AuditableEntityAttribute auditAttribute)
        {
            if (typeof(IAuditRecord).IsAssignableFrom(entityType))
            {
                if (entityType == auditAttribute.AuditEntryType) throw new AuditConfigurationException(entityType, "The audit record type {0} is marked for audit using itself. Did you mean to audit something else?", entityType.FullName);
                throw new AuditConfigurationException(entityType, "The audit record type {0} is marked for audit using {1}. Audit of audit records is not supported.", entityType.FullName, auditAttribute.AuditEntryType.FullName);
            }
            return new SimpleEntityModel(entityType, auditAttribute.AuditEntryType);
        }

        class SimpleEntityModel : IAuditableEntityModel
        {
            public Type EntityType { get; private set; }
            public Type AuditEntryType { get; private set; }

            public SimpleEntityModel(Type entityType, Type auditEntryType)
            {
                if (!typeof(IEntityAuditHistory).IsAssignableFrom(auditEntryType)) throw new AuditConfigurationException(entityType, "The type {0} does not implement {1}", auditEntryType.FullName, typeof(IEntityAuditHistory).Name);
                EntityType = entityType;
                AuditEntryType = auditEntryType;
            }
        }
    }
}
