using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Cfg;
using NHibernate.Mapping;

namespace Bluewire.NHibernate.Audit.Model
{
    public class AuditModelBuilder
    {
        public void AddSimpleType(PersistentClass classMapping)
        {
            simpleModels.Add(new SimpleEntityModel(classMapping.MappedClass, classMapping.MappedClass.GetAuditAttribute().AuditEntryType));
        }

        private readonly List<SimpleEntityModel> simpleModels = new List<SimpleEntityModel>();

        public void AddFromConfiguration(Configuration cfg)
        {
            var simpleTypes = cfg
                   .ClassMappings
                   .Where(m => m.MappedClass.IsAuditable())
                   .Where(m => !m.SubclassIterator.Any() && m.RootClazz == m);

            foreach (var t in simpleTypes) AddSimpleType(t);
        }

        public AuditModel GetValidatedModel(IAuditEntryFactory auditEntryFactory)
        {
            auditEntryFactory.AssertConfigurationIsValid();
            foreach (var model in simpleModels)
            {
                if (!auditEntryFactory.CanCreate(model.EntityType, model.AuditEntryType)) throw new AuditConfigurationException(model.EntityType, String.Format("Don't know how to create a {0} from this entity type.", model.AuditEntryType.FullName));
            }
            return new AuditModel(auditEntryFactory, simpleModels);
        }

        class SimpleEntityModel : IAuditableEntityModel
        {
            public Type EntityType { get; private set; }
            public Type AuditEntryType { get; private set; }

            public SimpleEntityModel(Type entityType, Type auditEntryType)
            {
                if (!typeof(IAuditHistory).IsAssignableFrom(auditEntryType)) throw new AuditConfigurationException(entityType, String.Format("The type {0} does not implement IAuditHistory", auditEntryType.FullName));
                EntityType = entityType;
                AuditEntryType = auditEntryType;
            }
        }
    }
}