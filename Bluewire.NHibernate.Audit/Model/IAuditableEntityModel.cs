using System;

namespace Bluewire.NHibernate.Audit.Model
{
    public interface IAuditableEntityModel
    {
        Type EntityType { get; }
        Type AuditEntryType { get;  }
    }
}