using System;

namespace Bluewire.NHibernate.Audit
{
    public class AuditConfigurationException : Exception
    {
        public AuditConfigurationException(Type entityType, string message) : base(String.Format("Cannot audit entity type {0}: {1}", entityType.FullName, message))
        {
        }
    }
}