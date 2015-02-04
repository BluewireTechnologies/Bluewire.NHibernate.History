using System;

namespace Bluewire.NHibernate.Audit.Model
{
    public interface IAuditEntryFactory
    {
        void AssertConfigurationIsValid();
        bool CanCreate(Type entityType, Type auditEntryType);
        IAuditHistory Create(object entity, Type entityType, Type auditEntryType);
        object CreateComponent(object component, Type componentType, Type auditValueType);
    }
}