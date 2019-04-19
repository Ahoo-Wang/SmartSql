﻿using SmartSql.Test.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SmartSql.Test.Unit.Deserializer
{
    [Collection("GlobalSmartSql")]
    public class DynamicDeserializerTest 
    {
        protected ISqlMapper SqlMapper { get; }

        public DynamicDeserializerTest(SmartSqlFixture smartSqlFixture)
        {
            SqlMapper = smartSqlFixture.SqlMapper;
        }
        [Fact]
        public void QuerySingle_Dynamic()
        {
            var result = SqlMapper.QuerySingle<dynamic>(new RequestContext
            {
                Scope = nameof(AllPrimitive),
                SqlId = "Query",
                Request = new { Taken = 1 }
            });
            Assert.NotEqual(0, result.Id);
        }
        [Fact]
        public void Query_Dynamic()
        {
            var result = SqlMapper.Query<dynamic>(new RequestContext
            {
                Scope = nameof(AllPrimitive),
                SqlId = "Query",
                Request = new { Taken = 10 }
            });
            Assert.NotEqual(0, result.FirstOrDefault().Id);
        }

        [Fact]
        public async Task QuerySingleAsync_Dynamic()
        {
            var result = await SqlMapper.QuerySingleAsync<dynamic>(new RequestContext
            {
                Scope = nameof(AllPrimitive),
                SqlId = "Query",
                Request = new { Taken = 1 }
            });
            Assert.NotEqual(0, result.Id);
        }
        [Fact]
        public async Task QueryAsync_Dynamic()
        {
            var result = await SqlMapper.QueryAsync<dynamic>(new RequestContext
            {
                Scope = nameof(AllPrimitive),
                SqlId = "Query",
                Request = new { Taken = 10 }
            });
            Assert.NotEqual(0, result.FirstOrDefault().Id);
        }
    }
}