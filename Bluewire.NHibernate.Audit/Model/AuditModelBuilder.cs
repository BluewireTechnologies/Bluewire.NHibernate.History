using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Bluewire.NHibernate.Audit.Meta;
using NHibernate.Cfg;
using NHibernate.Engine;
using NHibernate.Mapping;
using NHibernate.Type;

namespace Bluewire.NHibernate.Audit.Model
{
    public class AuditModelBuilder
    {
        public void AddSimpleType(PersistentClass classMapping)
        {
            var auditAttribute = classMapping.MappedClass.GetAuditAttribute();
            if (!classMapping.IsVersioned || classMapping.Version == null) throw new AuditConfigurationException(classMapping.MappedClass, "The NHibernate mapping for this type does not define a property to use for versioning.");
            simpleModels.Add(new SimpleEntityModel(classMapping.MappedClass, auditAttribute.AuditEntryType));
        }

        private void TryAddCollection(IMapping allMappings, Collection mapping)
        {
            var propertyInfo = GetPropertyForCollection(mapping);
            if (propertyInfo.GetAuditRelationAttribute() != null) relationModels.Add(CreateRelationModel(propertyInfo, new InferredRelationAuditInfo(mapping), allMappings));
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
            return typeof(IEntityAuditHistory).IsAssignableFrom(type);
        }

        private readonly List<SimpleEntityModel> simpleModels = new List<SimpleEntityModel>();
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

            foreach (var c in cfg.CollectionMappings.Where(m => m.Owner.MappedClass.IsAuditable()))
            {
                TryAddCollection(allMappings, c);
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
                if (!typeof(IEntityAuditHistory).IsAssignableFrom(auditEntryType)) throw new AuditConfigurationException(entityType, String.Format("The type {0} does not implement IAuditHistory", auditEntryType.FullName));
                EntityType = entityType;
                AuditEntryType = auditEntryType;
            }
        }

        private IAuditableRelationModel CreateRelationModel(PropertyInfo propertyInfo, InferredRelationAuditInfo mappingInfo, IMapping allMappings)
        {
            var relationAttr = propertyInfo.GetAuditRelationAttribute();
            if (relationAttr == null) throw new AuditConfigurationException(propertyInfo.DeclaringType, String.Format("Property {0} of type {1} does not declare audit history.", propertyInfo.Name, propertyInfo.DeclaringType));

            var manyToOne = mappingInfo.ElementType as ManyToOneType;
            if (manyToOne == null)
            {
                var auditValueType = relationAttr.AuditValueType ?? mappingInfo.ElementType.ReturnedClass;
                return new AuditableRelationModel(mappingInfo.Role, relationAttr.AuditEntryType, auditValueType, new ComponentCollectionAuditValueResolver());
            }
            else
            {
                if (!manyToOne.IsReferenceToPrimaryKey) throw new InvalidOperationException("Cannot audit a many-to-many collection which uses a key property other than the primary key.");
                if (relationAttr.AuditValueType != null) throw new InvalidOperationException("Cannot override the audited value for a collection of entities. The primary key will always be used.");
                var auditValueType = manyToOne.GetIdentifierOrUniqueKeyType(allMappings).ReturnedClass;
                return new AuditableRelationModel(mappingInfo.Role, relationAttr.AuditEntryType, auditValueType, new ReferenceRelationAuditValueResolver(manyToOne));
            }
        }

        class AuditableRelationModel : IAuditableRelationModel
        {
            public AuditableRelationModel(string collectionRole, Type auditEntryType, Type auditValueType, IRelationAuditValueResolver relationAuditEntryResolver)
            {
                AuditValueResolver = relationAuditEntryResolver;
                CollectionRole = collectionRole;
                AuditEntryType = auditEntryType;
                AuditValueType = auditValueType;
            }

            public string CollectionRole { get; private set; }
            public Type AuditEntryType { get; private set; }
            public Type AuditValueType { get; private set; }
            public IRelationAuditValueResolver AuditValueResolver { get; private set; }
        }


        public class InferredRelationAuditInfo
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
                return KeyType == null
                    ? AuditEntryBaseDefinition.MakeGenericType(OwningEntityIdType.ReturnedClass, elementType)
                    : AuditEntryBaseDefinition.MakeGenericType(OwningEntityIdType.ReturnedClass, KeyType.ReturnedClass, elementType);
            }

            public string Role { get; private set; }
            public Type AuditEntryBaseDefinition { get; private set; }
            public IType KeyType { get; private set; }
            public IType OwningEntityIdType { get; private set; }
            public IType ElementType { get; private set; }
        }
    }
}