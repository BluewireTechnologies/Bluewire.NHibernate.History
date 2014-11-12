﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NHibernate.Cfg;
using NHibernate.Mapping;
using NHibernate.Type;

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
            if(propertyInfo.GetAuditRelationAttribute() != null) relationModels.Add(new AuditableRelationModel(propertyInfo, new InferredRelationAuditInfo(mapping)));
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
            return typeof(IKeyedRelationAuditHistory).IsAssignableFrom(type) || typeof(ISetRelationAuditHistory).IsAssignableFrom(type);
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

        class AuditableRelationModel : IAuditableRelationModel
        {
            public AuditableRelationModel(PropertyInfo propertyInfo, InferredRelationAuditInfo mappingInfo)
            {
                var relationAttr = propertyInfo.GetAuditRelationAttribute();
                if (relationAttr == null) throw new AuditConfigurationException(propertyInfo.DeclaringType, String.Format("Property {0} of type {1} does not declare audit history.", propertyInfo.Name, propertyInfo.DeclaringType));
            
                CollectionRole = mappingInfo.Role;
                AuditEntryType = relationAttr.AuditEntryType;
                AuditValueType = relationAttr.AuditValueType ?? mappingInfo.ElementType.ReturnedClass;
            }

            public string CollectionRole { get; private set; }
            public Type AuditEntryType { get; private set; }
            public Type AuditValueType { get; private set; }
        }

        class InferredRelationAuditInfo
        {
            public InferredRelationAuditInfo(Collection mapping)
            {
                Role = mapping.Role;
                ElementType = mapping.Element.Type;
                OwningEntityIdType = mapping.Owner.Identifier.Type;

                if (mapping.IsIndexed)
                {
                    AuditEntryBaseDefinition = typeof(KeyedRelationAuditHistoryEntry<,,>);
                    KeyType = mapping.Key.Type;
                    if (KeyType == null) throw new ArgumentException(String.Format("No key type for collection: {0}", mapping.Role));
                }
                else
                {
                    AuditEntryBaseDefinition = typeof(SetRelationAuditHistoryEntry<,>);
                }
            }

            public Type GetExpectedBaseType(Type elementType)
            {
                if (KeyType == null)
                {
                    return AuditEntryBaseDefinition.MakeGenericType(OwningEntityIdType.ReturnedClass, elementType);
                }
                else
                {
                    return AuditEntryBaseDefinition.MakeGenericType(OwningEntityIdType.ReturnedClass, KeyType.ReturnedClass, elementType);
                }
            }

            public string Role { get; private set; }
            public Type AuditEntryBaseDefinition { get; private set; }
            public IType KeyType { get; private set; }
            public IType OwningEntityIdType { get; private set; }
            public IType ElementType { get; private set; }
        }
    }
}