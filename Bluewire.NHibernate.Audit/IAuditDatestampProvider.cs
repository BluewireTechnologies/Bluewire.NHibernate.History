using System;

namespace Bluewire.NHibernate.Audit
{
    /// <summary>
    /// Provides the value of 'now' when auditing.
    /// </summary>
    /// <remarks>
    /// This exists to permit mocking the clock. Usually Bluewire.Common.Time.IClock provides this
    /// functionality but we want to avoid referencing that directly to eliminate the package dependency.
    /// </remarks>
    public interface IAuditDatestampProvider
    {
        DateTimeOffset GetDatestampForNow();
    }
}