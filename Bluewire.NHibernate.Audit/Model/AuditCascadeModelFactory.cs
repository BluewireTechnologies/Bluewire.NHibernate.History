using System;
using Bluewire.NHibernate.Audit.Attributes;
using NHibernate.Mapping;
using NHibernate.Type;

namespace Bluewire.NHibernate.Audit.Model
{
    public class AuditCascadeModelFactory
    {
        public IAuditableCascadeModel CreateCascadeModel(Type entityType, AuditableRelationAttribute relationAttr, Property property)
        {
            if (relationAttr == null) throw new ArgumentNullException(nameof(relationAttr));

            if (property.Value is OneToOne oneToOne)
            {
                return new AuditableOneToOneCascadeModel(entityType, property.PersistentClass.EntityName, property.Name, oneToOne.Type.ReturnedClass);
            }
            throw new AuditConfigurationException(entityType, $"Not a one-to-one relationship: {property.Name} on {entityType}");
        }

        class AuditableOneToOneCascadeModel : IAuditableCascadeModel
        {
            public AuditableOneToOneCascadeModel(Type parentType, string parentEntityName, string referencingProperty, Type childType)
            {
                ParentType = parentType ?? throw new ArgumentNullException(nameof(parentType));
                ParentEntityName = parentEntityName ?? throw new ArgumentNullException(nameof(parentEntityName));
                ReferencingProperty = referencingProperty ?? throw new ArgumentNullException(nameof(referencingProperty));
                ChildType = childType ?? throw new ArgumentNullException(nameof(childType));
            }

            public Type ParentType { get; }
            public string ParentEntityName { get; }
            public string ReferencingProperty { get; }
            public Type ChildType { get; }
            public Type AuditEntryType => null;
        }
    }
}
