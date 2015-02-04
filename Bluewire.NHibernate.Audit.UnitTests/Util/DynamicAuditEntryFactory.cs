using System;
using AutoMapper;
using Bluewire.NHibernate.Audit.Model;

namespace Bluewire.NHibernate.Audit.UnitTests.Util
{
    class DynamicAuditEntryFactory : IAuditEntryFactory
    {
        public void AssertConfigurationIsValid()
        {
        }

        public bool CanCreate(Type entityType, Type auditEntryType)
        {
            return true;
        }

        public IAuditHistory Create(object entity, Type entityType, Type auditEntryType)
        {
            return (IAuditHistory)Mapper.DynamicMap(entity, entityType, auditEntryType);
        }


        public object CreateComponent(object component, Type componentType, Type auditValueType)
        {
            return Mapper.DynamicMap(component, componentType, auditValueType);
        }
    }
}