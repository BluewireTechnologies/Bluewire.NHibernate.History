using System;
using System.Reflection;
using System.Reflection.Emit;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Mapping;
using NHibernate.Mapping.ByCode;
using NHibernate.Proxy.DynamicProxy;

namespace Bluewire.NHibernate.Audit
{
    public class SimpleEntityAuditMeta : IAuditMeta
    {
        private readonly PersistentClass persistentClass;
        private Type auditEntityType;

        public SimpleEntityAuditMeta(PersistentClass persistentClass)
        {
            this.persistentClass = persistentClass;
            auditEntityType = new ProxyFactory().CreateProxyType(persistentClass.MappedClass, typeof(IAuditHistory));
        }

        public HbmMapping GenerateMapping()
        {
            var mapping = new HbmMapping();
            mapping.Items = new[] { CreateClassMap() };
            return mapping;
        }

        private HbmClass CreateClassMap()
        {
            var classMapping = new HbmClass()
            {
                entityname = Name,
            };
            return classMapping;
        }

        public string Name
        {
            get { return persistentClass.EntityName + "AuditHistory"; }
        }
    }

    public class AuditHistoryProxyBuilder
    {
        private static AssemblyBuilder assembly;
        private static ModuleBuilder module;
        private TypeBuilder typeBuilder;

        static AuditHistoryProxyBuilder()
        {
            assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("AuditHistory"), AssemblyBuilderAccess.Run);
            module = assembly.DefineDynamicModule("Impl");
        }

        public AuditHistoryProxyBuilder(string typeName, Type baseType)
        {
            typeBuilder = module.DefineType(typeName, TypeAttributes.Class, baseType ?? typeof(object), new [] { typeof(IAuditHistory) });
        }

        public void AddProperty(string propertyName, Type propertyType)
        {
        }

        public void AddVersionProperty(string actualPropertyName, Type propertyType)
        {
        }

        public void AddIdProperty(string actualPropertyName, Type propertyType)
        {
        }

        public void AddDatestampProperty(string actualPropertyName, Type propertyType)
        {
        }

        public void EnsurePreviousVersionProperty(Type propertyType)
        {

        }
    }

}