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


        public object Create(object entry, Type relationAuditValueType)
        {
            if (relationAuditValueType.IsInstanceOfType(entry)) return entry;
            return Mapper.DynamicMap(entry, entry.GetType(), relationAuditValueType);
        }
    }
}