using System;

namespace Bluewire.NHibernate.Audit.Model
{
    public interface IAuditableEntityModel
    {
        Type EntityType { get; }
        Type AuditEntryType { get;  }
    }

    public interface IAuditableRelationModel
    {
        string CollectionRole { get; }
        Type AuditEntryType { get; }
        string OwnerKeyPropertyName { get; }
    }
}