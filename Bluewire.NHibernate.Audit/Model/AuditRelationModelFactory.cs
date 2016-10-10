using System;
using System.Linq;
using Bluewire.NHibernate.Audit.Attributes;
using NHibernate.Engine;
using NHibernate.Type;

namespace Bluewire.NHibernate.Audit.Model
{
    public class AuditRelationModelFactory
    {
        public IAuditableRelationModel CreateRelationModel(Type entityType, AuditableRelationAttribute relationAttr, InferredRelationAuditInfo mappingInfo, IMapping allMappings)
        {
            if (relationAttr == null) throw new ArgumentNullException("relationAttr");
            
            var manyToOne = mappingInfo.ElementType as ManyToOneType;
            if (manyToOne == null)
            {
                return CreateComponentRelationModel(entityType, relationAttr, mappingInfo);
            }
            else
            {
                return CreateEntityRelationModel(entityType, relationAttr, mappingInfo, allMappings, manyToOne);
            }
        }

        private static void CheckThatAuditEntryIsAppropriateForCollectionType(Type entityType, AuditableRelationAttribute relationAttr, InferredRelationAuditInfo mappingInfo)
        {
            if (!mappingInfo.RequiredAuditEntryInterface.IsAssignableFrom(relationAttr.AuditEntryType))
            {
                throw new AuditConfigurationException(entityType,
                    "The type {0} does not implement {1}, which is required for auditing this collection. Consider deriving from {2} or check {3}'s collection mappings.",
                    relationAttr.AuditEntryType, mappingInfo.RequiredAuditEntryInterface, mappingInfo.AuditEntryBaseDefinition, entityType);
            }
        }

        private static IAuditableRelationModel CreateEntityRelationModel(Type entityType, AuditableRelationAttribute relationAttr, InferredRelationAuditInfo mappingInfo, IMapping allMappings, ManyToOneType manyToOne)
        {
            CheckThatAuditEntryIsAppropriateForCollectionType(entityType, relationAttr, mappingInfo);
            if (!manyToOne.IsReferenceToPrimaryKey) throw new InvalidOperationException("Cannot audit a many-to-many collection which uses a key property other than the primary key.");
            if (relationAttr.AuditValueType != null) throw new InvalidOperationException("Cannot override the audited value for a collection of entities. The primary key will always be used.");
            var auditValueType = manyToOne.GetIdentifierOrUniqueKeyType(allMappings).ReturnedClass;

            var snapshotProperties = RitSnapshotPropertyModel32.CollectPropertiesOnType(relationAttr.AuditEntryType).ToArray();
            if(snapshotProperties.Length > 1) throw new AuditConfigurationException(entityType, "The collection audit record type {0} declares multiple [AuditInterval] properties. At present, only one is supported.", relationAttr.AuditEntryType.FullName);

            return new AuditableRelationModel(mappingInfo.Role, relationAttr.AuditEntryType, auditValueType, new ReferenceRelationAuditValueResolver(manyToOne)) { RitProperty = snapshotProperties.SingleOrDefault() };
        }

        public static IAuditableRelationModel CreateComponentRelationModel(Type entityType, AuditableRelationAttribute relationAttr, InferredRelationAuditInfo mappingInfo)
        {
            CheckThatAuditEntryIsAppropriateForCollectionType(entityType, relationAttr, mappingInfo);
            var auditValueType = DetermineAuditValueType(mappingInfo, relationAttr, entityType);

            var snapshotProperties = RitSnapshotPropertyModel32.CollectPropertiesOnType(relationAttr.AuditEntryType).ToArray();
            if(snapshotProperties.Length > 1) throw new AuditConfigurationException(entityType, "The collection audit record type {0} declares multiple [AuditInterval] properties. At present, only one is supported.", relationAttr.AuditEntryType.FullName);

            return new AuditableRelationModel(mappingInfo.Role, relationAttr.AuditEntryType, auditValueType, new ComponentCollectionAuditValueResolver()) { RitProperty = snapshotProperties.SingleOrDefault() };
        }

        public static Type DetermineAuditValueType(InferredRelationAuditInfo inferred, AuditableRelationAttribute attribute, Type entityType)
        {
            var specificBase = inferred.GetRecognisedBaseType(attribute.AuditEntryType);
            if (specificBase != null)
            {
                // The audit entry derives from one of our base classes. We can determine the audit value type from it.
                var detectedValueType = specificBase.GetGenericArguments().Last();
                if (attribute.AuditValueType == null) return detectedValueType;

                // We have an override too, so sanity-check it.
                if (!detectedValueType.IsAssignableFrom(attribute.AuditValueType))
                {
                    throw new AuditConfigurationException(entityType,
                        "The type {0} expects values of type {1} but {2} was explicitly specified.",
                        attribute.AuditEntryType, detectedValueType, attribute.AuditValueType);
                }
                return attribute.AuditValueType;
            }

            // Custom audit entry type. Can't verify the value type, so just accept whatever the user gave us.
            return attribute.AuditValueType ?? inferred.ElementType.ReturnedClass;
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

            public RitSnapshotPropertyModel32 RitProperty { get; set; }
        }
    }
}
