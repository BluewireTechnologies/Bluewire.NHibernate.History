using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bluewire.NHibernate.Audit.Meta;
using NHibernate;
using NUnit.Framework;
using NHibernate.Linq;

namespace Bluewire.NHibernate.Audit.UnitTests.Util
{
    public static class Verify
    {
        struct KeyPair
        {
            public object Id { get; set; }
            public object VersionId { get; set; }
        }

        public static void HistoryChain(ISession session)
        {
            var allAuditEntities = session.Query<IEntityAuditHistory>().ToList();

            var previousVersions = allAuditEntities.Where(a => a.PreviousVersionId != null).Select(a => new KeyPair { VersionId = a.PreviousVersionId, Id = a.Id }).Distinct().ToList();
            var versions = allAuditEntities.Select(a => new KeyPair { VersionId = a.VersionId, Id = a.Id }).Distinct().ToList();

            Assert.That(previousVersions.Except(versions), Is.Empty);
        }

    }
}
