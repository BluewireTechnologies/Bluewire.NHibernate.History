using System.Collections.Generic;
using System.Linq;
using Bluewire.IntervalTree;
using Bluewire.NHibernate.Audit.Model;
using NHibernate.Engine;
using NHibernate.Mapping;
using NHibernate.SqlCommand;
using NHibernate.Type;

namespace Bluewire.NHibernate.Audit.Listeners
{
    class RitEndpointUpdateHelper
    {
        private readonly ISessionFactoryImplementor factory;
        private readonly PersistentClass auditMapping;

        public RitEndpointUpdateHelper(ISessionFactoryImplementor factory, PersistentClass auditMapping)
        {
            this.factory = factory;
            this.auditMapping = auditMapping;
        }

        public Property GetUpperBoundProperty(RitSnapshotPropertyModel32 propertyModel)
        {
            return auditMapping.GetRecursiveProperty($"{propertyModel.Property.Name}.{nameof(RitEntry32.Upper)}");
        }

        public Property GetStatusProperty(RitSnapshotPropertyModel32 propertyModel)
        {
            return auditMapping.GetRecursiveProperty($"{propertyModel.Property.Name}.{nameof(RitEntry32.Status)}");
        }

        /// <summary>
        /// Try to mark the RIT node in need of update via bitwise operator, rather than straight update.
        /// If successful, returns true and the resulting command needn't specify a value for node status.
        /// If the status property's columnset is not amenable to bitwise updates, returns false; the command
        /// should send RitStatus.Invalid for the relevant parameter.
        /// </summary>
        /// <param name="updateBuilder"></param>
        /// <param name="statusProperty"></param>
        /// <returns></returns>
        public bool TryAddStatusUpdateToStatement(SqlUpdateBuilder updateBuilder, Property statusProperty)
        {
            var ritStatusColumnName = factory.ColumnName(statusProperty.ColumnIterator);

            var literalConverter = statusProperty.Type as PersistentEnumType;
            if (literalConverter != null)
            {
                var statusFlag = literalConverter.ObjectToSQLString(RitStatus.NodeNeedsUpdate, factory.Dialect);
                long _dummy;
                if(long.TryParse(statusFlag, out _dummy))
                {
                    // Optimisation: If the status column is mapped as a PersistentEnumType, it is subject to bitwise
                    // operations. Flag the row for minimal update.
                    updateBuilder.AddColumn(ritStatusColumnName, $"{ritStatusColumnName} | {statusFlag}");
                    return true;
                }
            }
            // Fallback: If we can't flag details, just force a full update.
            updateBuilder.AddColumn(ritStatusColumnName, statusProperty.Type);
            return false;
        }

        public RitCommandPopulator AddToUpdateBuilder(SqlUpdateBuilder updateBuilder, RitSnapshotPropertyModel32 propertyModel)
        {
            var upperBoundProperty = GetUpperBoundProperty(propertyModel);
            updateBuilder.AddColumns(factory.ColumnNames(upperBoundProperty.ColumnIterator), upperBoundProperty.Type);
            var statusProperty = GetStatusProperty(propertyModel);
            var needsDirectStatusUpdate = !TryAddStatusUpdateToStatement(updateBuilder, statusProperty);
            return new RitCommandPopulator(propertyModel, upperBoundProperty, statusProperty, needsDirectStatusUpdate);
        }
    }
}
