using System;
using System.Collections.Generic;
using System.Text;
using SmartSql.ConfigBuilder;
using SmartSql.Exceptions;
using Xunit;

namespace SmartSql.Test.Unit.ConfigBuilder
{
    public class XmlConfigLoaderTest
    {
        [Fact]
        public void Load()
        {
            var configLoader = new XmlConfigBuilder(ResourceType.File, "SmartSqlMapConfig.xml");
            var config = configLoader.Build();
        }
        [Fact]
        public void LoadFailTest()
        {
            try
            {
                var configLoader = new XmlConfigBuilder(ResourceType.File, "SmartSqlMapConfig2.xml");
                var config = configLoader.Build();
            }
            catch (SmartSqlException ex)
            {                  
                Assert.StartsWith( "Read Nodes must have Weight attribute",ex.Message);
            }
        }
    }
}
