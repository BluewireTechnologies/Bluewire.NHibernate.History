using System;
using NHibernate.Engine;

namespace Bluewire.NHibernate.Audit.Model
{
    public interface IAuditableEntityModel
    {
        Type EntityType { get; }
        Type AuditEntryType { get;  }
    }
}