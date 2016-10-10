using System;

namespace Bluewire.NHibernate.Audit.Attributes
{
    /// <summary>
    /// Identifity a RitEntry32 property on an audit record for use as a 'snapshot query assist', based on
    /// the specified SnapshotIntervalTree32&lt;DateTimeOffset&gt; implementation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class AuditIntervalAttribute : Attribute
    {
        public AuditIntervalAttribute(Type intervalDefinitionType)
        {
            if (intervalDefinitionType == null) throw new ArgumentNullException(nameof(intervalDefinitionType));
            IntervalDefinitionType = intervalDefinitionType;
        }

        public Type IntervalDefinitionType { get; }
    }
}
