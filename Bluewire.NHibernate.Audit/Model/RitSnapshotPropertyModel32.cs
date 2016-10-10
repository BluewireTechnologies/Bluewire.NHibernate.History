using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bluewire.IntervalTree;
using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.Model
{
    public class RitSnapshotPropertyModel32
    {
        public RitSnapshotPropertyModel32(PropertyInfo property, SnapshotIntervalTree32<DateTimeOffset> intervalTree)
        {
            if (property.PropertyType != typeof(RitEntry32))
            {
                throw new ArgumentException($"Property is not of type {typeof(RitEntry32)}: {property}");
            }
            Property = property;
            IntervalTree = intervalTree;
        }

        public PropertyInfo Property { get; private set; }
        public SnapshotIntervalTree32<DateTimeOffset> IntervalTree { get; private set; }

        public static IEnumerable<RitSnapshotPropertyModel32> CollectPropertiesOnType(Type type)
        {
            foreach(var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.PropertyType != typeof(RitEntry32)) continue;
                var attr = property.GetCustomAttributes<AuditIntervalAttribute>(true).SingleOrDefault();
                if (attr == null) continue;
                if (!typeof(SnapshotIntervalTree32<DateTimeOffset>).IsAssignableFrom(attr.IntervalDefinitionType))
                {
                    throw new ArgumentException($"Interval definition does not support DateTimeOffset: {attr.IntervalDefinitionType}");
                }
                var intervalDefinition = (SnapshotIntervalTree32<DateTimeOffset>)Activator.CreateInstance(attr.IntervalDefinitionType);
                yield return new RitSnapshotPropertyModel32(property, intervalDefinition);
            }
        }
    }
}
