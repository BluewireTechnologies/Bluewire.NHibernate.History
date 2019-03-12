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
            return CreateEntityModel(classMapping.MappedClass, auditAttribute);
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
