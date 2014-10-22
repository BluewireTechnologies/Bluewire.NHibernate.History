using NHibernate.Cfg.MappingSchema;
using NHibernate.Mapping.ByCode;

namespace Bluewire.NHibernate.Audit
{
    public interface IAuditMeta
    {
        string Name { get; }
        HbmMapping GenerateMapping();
    }
}