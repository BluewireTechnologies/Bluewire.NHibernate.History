using System;
using System.Collections.Generic;
using System.Linq;
using Bluewire.NHibernate.Audit.Attributes;
using NHibernate.Mapping;
using NHibernate.Mapping.ByCode;

namespace Bluewire.NHibernate.Audit
{
    public class AuditModel
    {
        public AuditModel()
        {
        }

        public void AddSimpleType(PersistentClass classMapping)
        {
            var attr = GetAuditAttributes(classMapping.MappedClass);

            simpleModels.Add(new SimpleEntityModel(classMapping.MappedClass, attr.Single().AuditEntryType));
        }

        private readonly List<SimpleEntityModel> simpleModels = new List<SimpleEntityModel>();

        private IEnumerable<AuditableEntityAttribute> GetAuditAttributes(Type type)
        {
            return type.GetCustomAttributes(typeof(AuditableEntityAttribute), true).Cast<AuditableEntityAttribute>();
        }

        public void AddMappings(ModelMapper mapper)
        {
            foreach (var model in simpleModels)
            {
                model.AddMapping(mapper);
            }
        }

        class SimpleEntityModel
        {
            public Type EntityType { get; private set; }
            public Type AuditEntryType { get; private set; }

            public SimpleEntityModel(Type entityType, Type auditEntryType)
            {
                EntityType = entityType;
                AuditEntryType = auditEntryType;
            }

            public void AddMapping(ModelMapper mapper)
            {
            }
        }
    }
}