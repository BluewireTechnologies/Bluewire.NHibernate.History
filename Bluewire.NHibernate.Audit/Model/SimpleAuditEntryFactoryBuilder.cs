using System;
using System.Collections.Generic;
using Bluewire.NHibernate.Audit.Meta;

namespace Bluewire.NHibernate.Audit.Model
{
    public class SimpleAuditEntryFactoryBuilder
    {
        private readonly Dictionary<TypePair, Func<object, IEntityAuditHistory>> entities = new Dictionary<TypePair, Func<object, IEntityAuditHistory>>();
        private readonly Dictionary<TypePair, Func<object, object>> components = new Dictionary<TypePair, Func<object, object>>();

        public SimpleAuditEntryFactoryBuilder MapEntity<TEntity, TAudit>(Func<TEntity, TAudit> map) where TAudit : IEntityAuditHistory
        {
            var pair = new TypePair(typeof(TEntity), typeof(TAudit));
            entities.Add(pair, x => map((TEntity)x));
            return this;
        }

        public SimpleAuditEntryFactoryBuilder MapComponent<TEntity, TAudit>(Func<TEntity, TAudit> map)
        {
            var pair = new TypePair(typeof(TEntity), typeof(TAudit));
            components.Add(pair, x => map((TEntity)x));
            return this;
        }

        public IAuditEntryFactory Build() => new Impl(new Dictionary<TypePair, Func<object, IEntityAuditHistory>>(entities), new Dictionary<TypePair, Func<object, object>>(components));

        struct TypePair
        {
            public TypePair(Type entityType, Type auditType)
            {
                EntityType = entityType;
                AuditType = auditType;
            }

            public Type EntityType { get; }
            public Type AuditType { get; }
        }

        class Impl : IAuditEntryFactory
        {
            private readonly IDictionary<TypePair, Func<object, IEntityAuditHistory>> entities;
            private readonly IDictionary<TypePair, Func<object, object>> components;

            public Impl(IDictionary<TypePair, Func<object, IEntityAuditHistory>> entities, IDictionary<TypePair, Func<object, object>> components)
            {
                this.entities = entities;
                this.components = components;
            }

            public void AssertConfigurationIsValid() { }

            public bool CanCreate(Type entityType, Type auditEntryType)
            {
                var pair = new TypePair(entityType, auditEntryType);
                return entities.ContainsKey(pair);
            }

            public IEntityAuditHistory Create(object entity, Type entityType, Type auditEntryType)
            {
                var pair = new TypePair(entityType, auditEntryType);
                if (!entities.TryGetValue(pair, out var map)) throw new NotSupportedException();
                return map(entity);
            }

            public object CreateComponent(object component, Type componentType, Type auditValueType)
            {
                var pair = new TypePair(componentType, auditValueType);
                if (!components.TryGetValue(pair, out var map)) throw new NotSupportedException();
                return map(component);
            }
        }
    }
}
