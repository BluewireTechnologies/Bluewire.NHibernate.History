using System;
using System.Diagnostics;
using Bluewire.NHibernate.Audit.Meta;
using NHibernate.Mapping;
using NHibernate.Type;

namespace Bluewire.NHibernate.Audit.Model
{
    public class InferredRelationAuditInfo
    {
        public InferredRelationAuditInfo(string role, IType owningEntityIdType, IType keyType)
        {
            if (owningEntityIdType == null) throw new ArgumentNullException("owningEntityIdType");
            if (keyType == null) throw new ArgumentException(String.Format("No key type for collection: {0}", role));
            Role = role;
            OwningEntityIdType = owningEntityIdType;
            KeyType = keyType;
            RequiredAuditEntryInterface = typeof(IKeyedRelationAuditHistory);
            AuditEntryBaseDefinition = typeof(KeyedRelationAuditHistoryEntry<,,>);
        }

        public InferredRelationAuditInfo(string role, IType owningEntityIdType)
        {
            if (owningEntityIdType == null) throw new ArgumentNullException("owningEntityIdType");
            Role = role;
            OwningEntityIdType = owningEntityIdType;
            RequiredAuditEntryInterface = typeof(ISetRelationAuditHistory);
            AuditEntryBaseDefinition = typeof(SetRelationAuditHistoryEntry<,>);
        }

        public Type GetRecognisedBaseType(Type actualAuditEntryType)
        {
            return GetGenericTypeByDefinition(actualAuditEntryType, AuditEntryBaseDefinition);
        }

        private static Type GetGenericTypeByDefinition(Type type, Type genericDefinition)
        {
            Debug.Assert(genericDefinition.IsGenericTypeDefinition);
            while (type != null)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == genericDefinition) return type;
                type = type.BaseType;
            }
            return null;
        }

        public static InferredRelationAuditInfo Analyse(Collection mapping)
        {
            if (mapping.IsIndexed)
            {
                return new InferredRelationAuditInfo(mapping.Role, mapping.Owner.Identifier.Type, mapping.Key.Type) { ElementType =  mapping.Element.Type };
            }
            else
            {
                return new InferredRelationAuditInfo(mapping.Role, mapping.Owner.Identifier.Type) { ElementType =  mapping.Element.Type };
            }
        }

        public string Role { get; private set; }
        public IType OwningEntityIdType { get; private set; }
        public IType KeyType { get; private set; }
        public IType ElementType { get; set; }

        public Type RequiredAuditEntryInterface { get; private set; }
        public Type AuditEntryBaseDefinition { get; private set; }
    }
}