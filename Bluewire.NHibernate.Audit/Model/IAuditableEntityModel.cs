using System;

namespace Bluewire.NHibernate.Audit.Model
{
    public interface IAuditableEntityModel : IAuditRecordModel
    {
        Type EntityType { get; }
    }
}
