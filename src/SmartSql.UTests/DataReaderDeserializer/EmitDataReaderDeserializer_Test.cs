﻿using SmartSql.Abstractions.Command;
using SmartSql.Abstractions.DataReaderDeserializer;
using SmartSql.Abstractions.DbSession;
using SmartSql.Abstractions.TypeHandler;
using SmartSql.DataReaderDeserializer;
using SmartSql.DbSession;
using System;
using Xunit;
using Microsoft.Extensions.Logging;
using SmartSql.Command;
using SmartSql.Abstractions;
using SmartSql.UTests.Entity;
using System.Linq;

namespace SmartSql.UTests.DataReaderDeserializer
{
    public class EmitDataReaderDeserializer_Test : TestBase, IDisposable
    {
        IDataReaderDeserializerFactory _deserializerFactory;
        IDbConnectionSessionStore _sessionStore;
        ICommandExecuter _commandExecuter;
        SmartSqlOptions  _smartSqlOptions;
        public EmitDataReaderDeserializer_Test()
        {
            _deserializerFactory = new EmitDataReaderDeserializerFactory();
            _smartSqlOptions = new SmartSqlOptions { DataReaderDeserializerFactory = _deserializerFactory };
            _smartSqlOptions.Setup();
            _sessionStore = _smartSqlOptions.DbSessionStore;
            _commandExecuter = new CommandExecuter(LoggerFactory.CreateLogger<CommandExecuter>(), _smartSqlOptions.PreparedCommand);
        }

        [Fact]
        public void Des()
        {
            RequestContext context = new RequestContext
            {
                Scope = Scope,
                SqlId = "Query",
                Request = new { Taken = 10 }
            };
            context.Setup(_smartSqlOptions);
            var dbSession = _sessionStore.CreateDbSession(DataSource);
            var result = _commandExecuter.ExecuteReader(dbSession, context);
            var wrapper = new DataReaderWrapper(result);
            var deser = _deserializerFactory.Create();
            var list = deser.ToEnumerable<T_Entity>(context, wrapper).ToList();
            result.Close();
            result.Dispose();
        }
        [Fact]
        public void QueryStatus()
        {
            RequestContext context = new RequestContext
            {
                Scope = Scope,
                SqlId = "QueryStatus",
            };
            context.Setup(_smartSqlOptions);
            var dbSession = _sessionStore.CreateDbSession(DataSource);
            var result = _commandExecuter.ExecuteReader(dbSession, context);
            var wrapper = new DataReaderWrapper(result);
            var deser = _deserializerFactory.Create();
            var userIds = deser.ToEnumerable<EntityStatus>(context, wrapper).ToList();
            result.Close();
            result.Dispose();
        }
        [Fact]
        public void QueryNullStatus()
        {
            RequestContext context = new RequestContext
            {
                Scope = Scope,
                SqlId = "QueryNullStatus",
            };
            context.Setup(_smartSqlOptions);
            var dbSession = _sessionStore.CreateDbSession(DataSource);
            var result = _commandExecuter.ExecuteReader(dbSession, context);
            var wrapper = new DataReaderWrapper(result);
            var deser = _deserializerFactory.Create();
            var userIds = deser.ToEnumerable<EntityStatus?>(context, wrapper).ToList();
            result.Close();
            result.Dispose();
        }



        public void Dispose()
        {
            _sessionStore.Dispose();
        }
    }
}
