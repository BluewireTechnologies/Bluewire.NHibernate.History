using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Bluewire.Common.Extensions;
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

        private void TryAddCollection(Collection mapping)
        {
            var propertyInfo = GetPropertyForCollection(mapping);
            if(propertyInfo.GetAuditRelationAttribute() != null) relationModels.Add(new ListRelationModel(propertyInfo, mapping.Role));
        }

        private PropertyInfo GetPropertyForCollection(Collection mapping)
        {
            var propertyName = mapping.Role.Replace(mapping.OwnerEntityName, "").TrimStart('.');
            var propertyInfo = mapping.Owner.MappedClass.GetProperty(propertyName);
            Debug.Assert(propertyInfo != null);
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

        private bool IsRelationEntryType(Type type)
        {
            return type.IsSubclassOf(typeof(ListElementAuditHistory));
        }
        private bool IsEntityEntryType(Type type)
        {
            return typeof(IAuditHistory).IsAssignableFrom(type);
        }

        private readonly List<SimpleEntityModel> simpleModels = new List<SimpleEntityModel>();
        private readonly List<IAuditableRelationModel> relationModels = new List<IAuditableRelationModel>();
        private readonly List<PersistentClass> auditEntryMappings = new List<PersistentClass>();

        public void AddFromConfiguration(Configuration cfg)
        {
            var simpleTypes = cfg
                   .ClassMappings
                   .Where(m => m.MappedClass.IsAuditable())
                   .Where(m => !m.SubclassIterator.Any() && m.RootClazz == m);

            foreach (var c in cfg.CollectionMappings.Where(m => m.Owner.MappedClass.IsAuditable()))
            {
                TryAddCollection(c);
            }

            var relationAuditTypes = cfg.ClassMappings.Where(m => IsRelationEntryType(m.MappedClass));
            var entityAuditTypes = cfg.ClassMappings.Where(m => IsEntityEntryType(m.MappedClass));

            foreach (var t in simpleTypes) AddSimpleType(t);
            foreach (var t in relationAuditTypes) AddAuditEntryType(t);
            foreach (var t in entityAuditTypes) AddAuditEntryType(t);
        }

        public AuditModel GetValidatedModel(IAuditEntryFactory auditEntryFactory)
        {
            auditEntryFactory.AssertConfigurationIsValid();
            foreach (var model in simpleModels)
            {
                if (!auditEntryFactory.CanCreate(model.EntityType, model.AuditEntryType)) throw new AuditConfigurationException(model.EntityType, String.Format("Don't know how to create a {0} from this entity type.", model.AuditEntryType.FullName));
            }


            return new AuditModel(auditEntryFactory, simpleModels, relationModels, auditEntryMappings);
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

        class ListRelationModel : IAuditableRelationModel
        {
            public ListRelationModel(PropertyInfo propertyInfo, string role)
            {
                var relationAttr = propertyInfo.GetAuditRelationAttribute();
                if (relationAttr == null) throw new AuditConfigurationException(propertyInfo.DeclaringType, String.Format("Property {0} of type {1} does not declare audit history.", propertyInfo.Name, propertyInfo.DeclaringType));
            
                CollectionRole = role;
                AuditEntryType = relationAttr.AuditEntryType;
                OwnerKeyPropertyName = relationAttr.OwnerKeyPropertyName;
            }

            public string CollectionRole { get; private set; }

            public Type AuditEntryType { get; private set; }

            public string OwnerKeyPropertyName { get; private set; }
        }
    }
}