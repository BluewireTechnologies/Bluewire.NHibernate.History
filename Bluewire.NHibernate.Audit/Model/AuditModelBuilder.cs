using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Bluewire.NHibernate.Audit.Meta;
using NHibernate.Cfg;
using NHibernate.Engine;
using NHibernate.Mapping;

namespace Bluewire.NHibernate.Audit.Model
{
    public class AuditModelBuilder
    {
        public void AddEntityType(PersistentClass classMapping)
        {
            var auditAttribute = classMapping.MappedClass.GetAuditAttribute();
            entityModels.Add(new AuditEntityModelFactory().CreateEntityModel(classMapping, auditAttribute));
        }

        private void TryAddCollection(IMapping allMappings, Collection mapping)
        {
            var propertyInfo = GetPropertyForCollection(mapping);
            if (propertyInfo == null)
            {
                // If we can't find the property, then presumably the mapping is invalid. But if NHibernate doesn't complain
                // then this is a potential bug in Bluewire.NHibernate.Audit.
                return;
            }
            var relationAttr = propertyInfo.GetAuditRelationAttribute();
            if (relationAttr != null)
            {
                relationModels.Add(new AuditRelationModelFactory().CreateRelationModel(propertyInfo.DeclaringType, relationAttr, InferredRelationAuditInfo.Analyse(mapping), allMappings));
            }
        }

        private PropertyInfo GetPropertyForCollection(Collection mapping)
        {
            var propertyName = mapping.Role.Replace(mapping.OwnerEntityName, "").TrimStart('.');
            var propertyInfo = mapping.Owner.MappedClass.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            Debug.Assert(propertyInfo != null, $"Unable to find a property called {propertyName} on {mapping.Owner.MappedClass}.");
            return propertyInfo;
        }

        public void AddAuditEntryType(PersistentClass classMapping)
        {
            if (!IsRelationEntryType(classMapping.MappedClass) && !IsEntityEntryType(classMapping.MappedClass))
            {
                throw new ArgumentException(String.Format("Not an audit-related type: {0}", classMapping.MappedClass.FullName));
            }
            auditEntryMappings.Add(classMapping);
        }

        private static bool IsRelationEntryType(Type type)
        {
            return typeof(IKeyedRelationAuditHistory).IsAssignableFrom(type) || typeof(ISetRelationAuditHistory).IsAssignableFrom(type);
        }
        private static bool IsEntityEntryType(Type type)
        {
            return typeof(IEntityAuditHistory).IsAssignableFrom(type);
        }

        private readonly List<IAuditableEntityModel> entityModels = new List<IAuditableEntityModel>();
        private readonly List<IAuditableRelationModel> relationModels = new List<IAuditableRelationModel>();
        private readonly List<PersistentClass> auditEntryMappings = new List<PersistentClass>();

        public void AddFromConfiguration(Configuration cfg)
        {
            cfg.BuildMappings(); // Must do this first, to get collection information.

            var allMappings = cfg.BuildMapping();

            var simpleTypes = cfg
                   .ClassMappings
                   .Where(m => m.MappedClass.IsAuditable())
                   .Where(m => !m.SubclassIterator.Any() && m.RootClazz == m);

            var subclassTypes = cfg
                   .ClassMappings
                   .Where(m => m.MappedClass.IsAuditable())
                   .Where(m => !m.MappedClass.IsAbstract)
                   .Where(m => m.RootClazz != m);

            foreach (var t in simpleTypes) AddEntityType(t);
            foreach (var t in subclassTypes) AddEntityType(t);

            foreach (var c in cfg.CollectionMappings.Where(m => m.Owner.MappedClass.IsAuditable()))
            {
                TryAddCollection(allMappings, c);
            }

            var relationAuditTypes = cfg.ClassMappings.Where(m => IsRelationEntryType(m.MappedClass));
            var entityAuditTypes = cfg.ClassMappings.Where(m => IsEntityEntryType(m.MappedClass));

            foreach (var t in relationAuditTypes) AddAuditEntryType(t);
            foreach (var t in entityAuditTypes) AddAuditEntryType(t);
        }

        public AuditModel GetValidatedModel(IAuditEntryFactory auditEntryFactory)
        {
            auditEntryFactory.AssertConfigurationIsValid();
            foreach (var model in entityModels)
            {
                if (!auditEntryFactory.CanCreate(model.EntityType, model.AuditEntryType)) throw new AuditConfigurationException(model.EntityType, String.Format("Don't know how to create a {0} from this entity type.", model.AuditEntryType.FullName));
            }

            return new AuditModel(auditEntryFactory, entityModels, relationModels, auditEntryMappings);
        }
    }
}