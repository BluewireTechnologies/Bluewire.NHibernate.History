using System;
using Bluewire.IntervalTree;
using Bluewire.NHibernate.Audit.Model;
using Bluewire.NHibernate.Audit.Support;
using NHibernate.Mapping;

namespace Bluewire.NHibernate.Audit.Listeners
{
    /// <summary>
    /// Encapsulates assignment of upper bound property and possibly the status property of a RitEntry32, for use
    /// when constructing an UPDATE command.
    /// </summary>
    class RitCommandPopulator
    {
        private readonly RitSnapshotPropertyModel32 intervalPropertyModel;
        private readonly Property upperBoundProperty;
        private readonly Property statusProperty;
        private readonly bool needsDirectStatusUpdate;

        public RitCommandPopulator(RitSnapshotPropertyModel32 intervalPropertyModel, Property upperBoundProperty, Property statusProperty, bool needsDirectStatusUpdate)
        {
            this.intervalPropertyModel = intervalPropertyModel;
            this.upperBoundProperty = upperBoundProperty;
            this.statusProperty = statusProperty;
            this.needsDirectStatusUpdate = needsDirectStatusUpdate;
        }

        public void ApplyParameters(CommandParameteriser parameters, DateTimeOffset operationDatestamp)
        {
            var upperBound = intervalPropertyModel.IntervalTree.GetUpperBound(operationDatestamp);
            parameters.Set(upperBoundProperty, upperBound);
            if (needsDirectStatusUpdate)
            {
                parameters.Set(statusProperty, RitStatus.Invalid);
            }
        }
    }
}
