using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bluewire.NHibernate.Audit.Meta;
using Bluewire.NHibernate.Audit.Model;
using Bluewire.NHibernate.Audit.Support;
using NHibernate.AdoNet;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Mapping;
using NHibernate.SqlCommand;

namespace Bluewire.NHibernate.Audit.Listeners
{
    public class Rit32Tasks
    {
        private readonly AuditModel model;

        public Rit32Tasks(AuditModel model)
        {
            this.model = model;
        }

        public void AssignRitEntry32ForNewRecord(IAuditRecord record, RitSnapshotPropertyModel32 intervalPropertyModel, DateTimeOffset now)
        {
            var ritEntry = intervalPropertyModel.IntervalTree.CalculateNodeWithoutEnd(now);
            intervalPropertyModel.Property.SetValue(record, ritEntry);
        }

        public void UpdateRitEntry32ForPreviousEntityRecord(IEventSource session, IEntityAuditHistory newEntry, RitSnapshotPropertyModel32 intervalPropertyModel, DateTimeOffset operationDatestamp)
        {
            var auditMapping = model.GetAuditClassMapping(newEntry.GetType());

            var auditDelete = new AuditEntityRit32UpperBoundCommand(session.Factory, auditMapping, intervalPropertyModel);
            var expectation = Expectations.AppropriateExpectation(ExecuteUpdateResultCheckStyle.Count);
            var cmd = session.Batcher.PrepareBatchCommand(auditDelete.Command.CommandType, auditDelete.Command.Text, auditDelete.Command.ParameterTypes);
            auditDelete.PopulateCommand(session, cmd, newEntry.PreviousVersionId, operationDatestamp);
            session.Batcher.AddToBatch(expectation);
        }

        /// <summary>
        /// Generates a command of the form:
        ///     update (audit table) set (upper) = ?, (status) |= RitStatus.NodeNeedsUpdate where (versionId) = ?
        /// 
        /// Implementations are expected to add the necessary predicates for the collection type.
        /// </summary>
        class AuditEntityRit32UpperBoundCommand
        {
            private readonly ISessionFactoryImplementor factory;
            private readonly Property versionIdProperty;
            private readonly RitCommandPopulator ritPopulater;

            public AuditEntityRit32UpperBoundCommand(ISessionFactoryImplementor factory, PersistentClass auditMapping, RitSnapshotPropertyModel32 propertyModel)
            {
                this.factory = factory;
                SqlUpdateBuilder = new SqlUpdateBuilder(factory.Dialect, factory);
                versionIdProperty = auditMapping.PropertyClosureIterator.Single(n => n.Name == "VersionId");

                SqlUpdateBuilder.SetTableName(auditMapping.Table.GetQualifiedName(factory.Dialect, factory.Settings.DefaultCatalogName, factory.Settings.DefaultSchemaName));

                var ritHelper = new RitEndpointUpdateHelper(factory, auditMapping);
                ritPopulater = ritHelper.AddToUpdateBuilder(SqlUpdateBuilder, propertyModel);

                AddPredicateProperty(versionIdProperty);
            }

            private SqlUpdateBuilder SqlUpdateBuilder { get; }

            private void AddPredicateProperty(Property property)
            {
                SqlUpdateBuilder.AddWhereFragment(ColumnNames(factory, property.ColumnIterator), property.Type, " = ");
            }

            private static string[] ColumnNames(ISessionFactoryImplementor factory, IEnumerable<ISelectable> columnIterator)
            {
                return columnIterator.Select(k => k.GetText(factory.Dialect)).ToArray();
            }

            public SqlCommandInfo Command => SqlUpdateBuilder.ToSqlCommandInfo();

            public void PopulateCommand(ISessionImplementor session, IDbCommand cmd, object versionId, DateTimeOffset operationDatestamp)
            {
                var parameters = new CommandParameteriser(session, cmd);
                ritPopulater.ApplyParameters(parameters, operationDatestamp);
                parameters.Set(versionIdProperty, versionId);
            }
        }
    }
}
