﻿using Microsoft.Extensions.Logging;
using SmartSql.Abstractions.DataSource;
using SmartSql.Configuration;
using SmartSql.Logging;
using System.Data.Common;
using System.Reflection;
using Xunit;
using System.Collections;
using System.Collections.Generic;
using SmartSql.UTests.Entity;
using System;
using System.Runtime.CompilerServices;

namespace SmartSql.UTests
{
    public class TestBase
    {
        public string SqlMapConfigFilePath { get { return "SmartSqlMapConfig.xml"; } }
        public DbProviderFactory DbProviderFactory { get; private set; }
        public ILoggerFactory LoggerFactory { get { return new NoneLoggerFactory(); } }
        public virtual string Scope { get { return "Entity"; } }
        public string ConnectionString { get { return "Data Source=.;database=SmartSqlStarterDB;uid=sa;pwd=SmartSql.Net!"; } }
        public IDataSource DataSource
        {
            get
            {
                return new WriteDataSource
                {
                    Name = "Default",
                    ConnectionString = ConnectionString
                };
            }
        }
        public TestBase()
        {
            string assemblyName = "System.Data.SqlClient";
            string typeName = "System.Data.SqlClient.SqlClientFactory";
            DbProviderFactory = Assembly.Load(new AssemblyName { Name = assemblyName })
                              .GetType(typeName)
                              .GetField("Instance")
                              .GetValue(null) as DbProviderFactory;
        }

    }
}
