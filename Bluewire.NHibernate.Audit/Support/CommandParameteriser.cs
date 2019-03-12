using System.Data;
using NHibernate.Engine;
using NHibernate.Mapping;

namespace Bluewire.NHibernate.Audit.Support
{
    /// <summary>
    /// Convenience class which tracks column spans of properties set on commands.
    /// Still requires that the order in which properties are set matches the
    /// order they are defined in the command, though.
    /// </summary>
    class CommandParameteriser
    {
        private readonly ISessionImplementor session;
        private readonly IDbCommand cmd;
        private int i;

        public CommandParameteriser(ISessionImplementor session, IDbCommand cmd)
        {
            this.session = session;
            this.cmd = cmd;
            i = 0;
        }

        public CommandParameteriser Set(Property property, object value)
        {
            property.Type.NullSafeSet(cmd, value, i, session);
            i+= property.ColumnSpan;
            return this;
        }
    }
}
