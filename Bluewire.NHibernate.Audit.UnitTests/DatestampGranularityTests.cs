using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bluewire.Common.Time;
using Bluewire.NHibernate.Audit.Runtime;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests
{
    [TestFixture]
    public class DatestampGranularityTests 
    {
        [Test]
        public void DatestampRoundtrippedThroughISOFormatStringIsStillSufficientlyAccurate()
        {
            var date = DateTimeOffset.Now.Date;
            var time = new TimeSpan(11, 30, 45);

            var timeMillisecondAccuracy = time.Add(TimeSpan.FromMilliseconds(233));
            var timeNanosecondAccuracy = timeMillisecondAccuracy.Add(TimeSpan.FromTicks(9578));

            Assume.That(timeMillisecondAccuracy, Is.LessThan(timeNanosecondAccuracy));
            Assume.That(timeMillisecondAccuracy + TimeSpan.FromMilliseconds(1), Is.GreaterThan(timeNanosecondAccuracy));

            var clock = new MockClock(new DateTimeOffset(date + timeNanosecondAccuracy));

            var auditInfo = new SessionAuditInfo(clock);

            var serialised = auditInfo.OperationDatestamp.ToString(Serialisation.AuditDatestampFormat);
            var roundtripped = DateTimeOffset.Parse(serialised);

            // This is required to hold true in order for a roundtripped datestamp to include the record it refers to when used as a snapshot point.
            Assert.That(auditInfo.OperationDatestamp, Is.LessThanOrEqualTo(roundtripped));
        }
    }
}
