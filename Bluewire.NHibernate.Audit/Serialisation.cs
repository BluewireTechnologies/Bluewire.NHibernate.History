using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bluewire.NHibernate.Audit
{
    public static class Serialisation
    {
        /// <summary>
        /// Defines standard Audit Datestamp serialisation format which preserves millisecond accuracy.
        /// </summary>
        public const string AuditDatestampFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffzzz";
    }
}
