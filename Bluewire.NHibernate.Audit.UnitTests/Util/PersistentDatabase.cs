﻿using System;
using System.IO;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Tool.hbm2ddl;

namespace Bluewire.NHibernate.Audit.UnitTests.Util
{
    /// <summary>
    /// Temporary file-backed test database instance.
    /// </summary>
    /// <remarks>
    /// Retains data between sessions. Useful when testing interactions between sessions.
    /// </remarks>
    public class PersistentDatabase : IDisposable
    {
        private readonly string dbFileName;
        private readonly Configuration cfg;
        private ISessionFactory sessionFactory;

        private PersistentDatabase()
        {
            // required for the database to persist between sessions:
            dbFileName = Path.GetTempFileName();
            cfg = new Configuration();
            cfg.DataBaseIntegration(d =>
            {
                d.Dialect<SQLiteDialect>();
                d.ConnectionString = String.Format("Data Source={0};Version=3", dbFileName);
            });
        }

        public static PersistentDatabase Configure(Action<Configuration> configure)
        {
            var db = new PersistentDatabase();
            db.InitConfiguration(configure);
            return db;
        }

        private void InitConfiguration(Action<Configuration> configure)
        {
            configure(cfg);
            SchemaMetadataUpdater.QuoteTableAndColumns(cfg);
            sessionFactory = cfg.BuildSessionFactory();
            new SchemaExport(cfg).Create(false, true);
        }

        public ISession CreateSession()
        {
            var session = sessionFactory.OpenSession();
            return session;
        }

        public void Dispose()
        {
            sessionFactory.Dispose();
            File.Delete(dbFileName);
        }
    }
}
