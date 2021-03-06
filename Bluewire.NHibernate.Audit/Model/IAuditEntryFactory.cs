﻿using System;
using Bluewire.NHibernate.Audit.Meta;

namespace Bluewire.NHibernate.Audit.Model
{
    public interface IAuditEntryFactory
    {
        void AssertConfigurationIsValid();
        bool CanCreate(Type entityType, Type auditEntryType);
        IEntityAuditHistory Create(object entity, Type entityType, Type auditEntryType);
        object CreateComponent(object component, Type componentType, Type auditValueType);
    }
}
