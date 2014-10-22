using System;
using System.Collections.Generic;
using System.Linq;
using Bluewire.NHibernate.Audit.Attributes;
using NHibernate.Cfg;
using NHibernate.Mapping;
using NHibernate.Mapping.ByCode;

namespace Bluewire.NHibernate.Audit
{
    public class AuditModelMappingGenerator
    {
        public void AddGeneratedMappings(Configuration configuration)
        {
            var model = new AuditModel();
            
            var simpleTypes = configuration
                .ClassMappings
                .Where(m => GetAuditAttributes(m.MappedClass).Any())
                .Where(m => !m.SubclassIterator.Any() && m.RootClazz == null);

            foreach (var t in simpleTypes) model.AddSimpleType(t);

            var mapper = new ModelMapper();
            model.AddMappings(mapper);
            configuration.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            //foreach (var mapping in CollectMappings(configuration))
            //{
            //    configuration.AddDeserializedMapping(mapping.GenerateMapping(), mapping.Name);
            //}
        }

        private IEnumerable<AuditableEntityAttribute> GetAuditAttributes(Type type)
        {
            return type.GetCustomAttributes(typeof(AuditableEntityAttribute), true).Cast<AuditableEntityAttribute>();
        }

        public IEnumerable<IAuditMeta> CollectMappings(Configuration configuration)
        {
            return configuration.ClassMappings.Where(m => !m.SubclassIterator.Any() && m.RootClazz == null).Select(CreateSimpleAuditModel);
        }

        private SimpleEntityAuditMeta CreateSimpleAuditModel(PersistentClass classMapping)
        {
            return new SimpleEntityAuditMeta(classMapping);
        }

    }
}