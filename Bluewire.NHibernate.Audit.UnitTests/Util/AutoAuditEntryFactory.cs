using System;
using AutoMapper;
using AutoMapper.Configuration;
using Bluewire.NHibernate.Audit.Meta;
using Bluewire.NHibernate.Audit.Model;

namespace Bluewire.NHibernate.Audit.UnitTests.Util
{
    class AutoAuditEntryFactory : IAuditEntryFactory
    {
        private readonly Mapper mapper;

        public AutoAuditEntryFactory(Action<MapperConfigurationExpression> configure)
        {
            var configurationExpression = new MapperConfigurationExpression();
            configure(configurationExpression);
            var configurationStore = new MapperConfiguration(configurationExpression);
            configurationStore.AssertConfigurationIsValid();
            mapper = new Mapper(configurationStore);
        }

        public void AssertConfigurationIsValid()
        {
        }

        public bool CanCreate(Type entityType, Type auditEntryType)
        {
            return true;
        }

        public IEntityAuditHistory Create(object entity, Type entityType, Type auditEntryType)
        {
            return (IEntityAuditHistory)mapper.Map(entity, entityType, auditEntryType);
        }


        public object CreateComponent(object component, Type componentType, Type auditValueType)
        {
            return mapper.Map(component, componentType, auditValueType);
        }
    }

    public static class AutoAuditEntryFactoryExtensions
    {
        public static void IgnoreHistoryMetadata<T, THistory>(this IMappingExpression<T, THistory> expression) where THistory : IEntityAuditHistory
        {
            expression
                .ForMember(x => x.VersionId, opt => opt.Ignore())
                .ForMember(x => x.PreviousVersionId, opt => opt.Ignore())
                .ForMember(x => x.AuditId, opt => opt.Ignore())
                .ForMember(x => x.AuditDatestamp, opt => opt.Ignore())
                .ForMember(x => x.AuditedOperation, opt => opt.Ignore());
        }
    }
}
