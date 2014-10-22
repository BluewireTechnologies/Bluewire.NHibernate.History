using NHibernate.Proxy.DynamicProxy;

namespace Bluewire.NHibernate.Audit
{
    public class AuditEntryInterceptorImpl : IInterceptor
    {
        public object Intercept(InvocationInfo info)
        {
            return info.TargetMethod.Invoke(info.Target, info.Arguments);
        }
    }
}