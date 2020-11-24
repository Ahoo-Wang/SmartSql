using System;
using System.Collections.Generic;
using SmartSql.Configuration;
using SmartSql.Utils;
using System.Xml;
using SmartSql.DataSource;
using SmartSql.Exceptions;
using SmartSql.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SmartSql.AutoConverter;
using SmartSql.IdGenerator;

namespace SmartSql.ConfigBuilder
{
    public class XmlConfigBuilder : AbstractConfigBuilder
    {
        private readonly ResourceType _resourceType;
        private readonly string _resourcePath;
        private readonly ILoggerFactory _loggerFactory;
        public const String SMART_SQL_CONFIG_NAMESPACE = "http://SmartSql.net/schemas/SmartSqlMapConfig.xsd";
        public const String CONFIG_PREFIX = "Config";
        public const String TYPE_ATTRIBUTE = "Type";
        protected XmlDocument XmlConfig { get; private set; }
        protected XmlNamespaceManager XmlNsManager { get; private set; }
        protected XmlNode XmlConfigRoot { get; private set; }
        private readonly IIdGeneratorBuilder _idGeneratorBuilder = new IdGeneratorBuilder();

        private readonly IWordsConverterBuilder _wordsConverterBuilder = new WordsConverterBuilder();
        private readonly ITokenizerBuilder _tokenizerBuilder = new TokenizerBuilder();

        public XmlConfigBuilder(ResourceType resourceType, string resourcePath, ILoggerFactory loggerFactory = null)
        {
            _resourceType = resourceType;
            _resourcePath = resourcePath;
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            Logger = _loggerFactory.CreateLogger<XmlConfigBuilder>();
        }

        public override void Dispose()
        {

        }
        protected override void OnBeforeBuild()
        {
            XmlConfig = ResourceUtil.LoadAsXml(_resourceType, _resourcePath);
            XmlNsManager = new XmlNamespaceManager(XmlConfig.NameTable);
            XmlNsManager.AddNamespace(CONFIG_PREFIX, SMART_SQL_CONFIG_NAMESPACE);
            XmlConfigRoot = XmlConfig.SelectSingleNode($"/{CONFIG_PREFIX}:SmartSqlMapConfig", XmlNsManager);
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug($"XmlConfigBuilder Build ->> ResourceType:[{_resourceType}] , Path :[{_resourcePath}] Starting.");
            }
        }
        protected override void OnAfterBuild()
        {
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug($"XmlConfigBuilder Build ->> ResourceType:[{_resourceType}] , Path :[{_resourcePath}] End.");
            }
        }

        #region  0. Settings
        protected override void BuildSettings()
        {
            var settingsXPath = $"{CONFIG_PREFIX}:Settings";
            var settingsNode = XmlConfigRoot.SelectSingleNode(settingsXPath, XmlNsManager);
            if (settingsNode == null)
            {
                return;
            }
            if (settingsNode.Attributes.TryGetValueAsBoolean(nameof(Settings.IgnoreParameterCase),
                    out bool ignoreParameterCase))
            {
                SmartSqlConfig.Settings.IgnoreParameterCase = ignoreParameterCase;
            }
            if (settingsNode.Attributes.TryGetValueAsBoolean(nameof(Settings.IsCacheEnabled),
                out bool isCacheEnabled))
            {
                SmartSqlConfig.Settings.IsCacheEnabled = isCacheEnabled;
            }
            if (settingsNode.Attributes.TryGetValueAsString(nameof(Settings.ParameterPrefix),
                out string parameterPrefix))
            {
                SmartSqlConfig.Settings.ParameterPrefix = parameterPrefix;
            }
            if (settingsNode.Attributes.TryGetValueAsBoolean(nameof(Settings.EnablePropertyChangedTrack),
                out bool enableTrack))
            {
                SmartSqlConfig.Settings.EnablePropertyChangedTrack = enableTrack;
            }
            if (settingsNode.Attributes.TryGetValueAsBoolean(nameof(Settings.IgnoreDbNull),
                out bool ignoreDbNull))
            {
                SmartSqlConfig.Settings.IgnoreDbNull = ignoreDbNull;
            }
        }
        #endregion
        #region 1. Properties
        protected override void BuildProperties()
        {
            var properties = ParseProperties(XmlConfigRoot);
            SmartSqlConfig.Properties.Import(properties);
        }
        #endregion
        #region 2. IdGen
        protected override void BuildIdGenerators()
        {
            var idGenXPath = $"{CONFIG_PREFIX}:IdGenerators/{CONFIG_PREFIX}:IdGenerator";
            var idGenNodes = XmlConfigRoot.SelectNodes(idGenXPath, XmlNsManager);
            if (idGenNodes != null)
            {
                SmartSqlConfig.IdGenerators.Clear();
                foreach (XmlNode idGenNode in idGenNodes)
                {
                    BuildIdGenerator(idGenNode);
                }
            }
        }
        private void BuildIdGenerator(XmlNode idGenNode)
        {
            if (!idGenNode.Attributes.TryGetValueAsString("Name", out string name, SmartSqlConfig.Properties))
            {
                throw new SmartSqlException("IdGenerator.Name can not be null.");
            }
            if (!idGenNode.Attributes.TryGetValueAsString("Type", out string typeString, SmartSqlConfig.Properties))
            {
                throw new SmartSqlException("IdGenerator.Type can not be null.");
            }
            var parameters = ParseProperties(idGenNode);
            var idGen = _idGeneratorBuilder.Build(typeString, parameters);
            SmartSqlConfig.IdGenerators.Add(name, idGen);
        }
        #endregion
        #region 2. Database
        protected override void BuildDatabase()
        {
            Database database = new Database
            {
                Reads = new Dictionary<string, ReadDataSource>()
            };
            SmartSqlConfig.Database = database;
            var databaseXPath = $"{CONFIG_PREFIX}:Database";
            var databaseNode = XmlConfigRoot.SelectSingleNode(databaseXPath, XmlNsManager);
            if (databaseNode == null)
            {
                throw new SmartSqlException("Can not find Database Node.");
            }
            var dbProviderNode = databaseNode.SelectSingleNode($"{CONFIG_PREFIX}:DbProvider", XmlNsManager);
            database.DbProvider = ParseDbProvider(dbProviderNode);
            var writeDataSourceNode = databaseNode.SelectSingleNode($"{CONFIG_PREFIX}:Write", XmlNsManager);
            database.Write = ParseWriteDataSource(writeDataSourceNode);
            database.Write.DbProvider = database.DbProvider;
            var readDataSourceNodes = databaseNode.SelectNodes($"{CONFIG_PREFIX}:Read", XmlNsManager);
            if (readDataSourceNodes == null) return;
            var readDataSources = this.ParseReadDataSource(readDataSourceNodes, database.DbProvider);
            database.Reads.AddRange(readDataSources);
        }

        private DbProvider ParseDbProvider(XmlNode dbProviderNode)
        {
            if (dbProviderNode == null)
            {
                throw new SmartSqlException("DbProvider can not be null.");
            }
            DbProvider dbProvider = new DbProvider();
            if (dbProviderNode.Attributes.TryGetValueAsString(nameof(DbProvider.Name), out string name, SmartSqlConfig.Properties))
            {
                dbProvider.Name = name;
            }
            if (dbProviderNode.Attributes.TryGetValueAsString(nameof(DbProvider.ParameterPrefix), out string parameterPrefix, SmartSqlConfig.Properties))
            {
                dbProvider.ParameterPrefix = parameterPrefix;
            }
            if (dbProviderNode.Attributes.TryGetValueAsString(nameof(DbProvider.ParameterNamePrefix), out string namePrefix, SmartSqlConfig.Properties))
            {
                dbProvider.ParameterNamePrefix = namePrefix;
            }
            if (dbProviderNode.Attributes.TryGetValueAsString(nameof(DbProvider.ParameterNameSuffix), out string nameSuffix, SmartSqlConfig.Properties))
            {
                dbProvider.ParameterNameSuffix = nameSuffix;
            }
            if (dbProviderNode.Attributes.TryGetValueAsString(nameof(DbProvider.SelectAutoIncrement), out string selectAutoIncrement, SmartSqlConfig.Properties))
            {
                dbProvider.SelectAutoIncrement = selectAutoIncrement;
            }

            if (dbProviderNode.Attributes.TryGetValueAsString(TYPE_ATTRIBUTE, out string type, SmartSqlConfig.Properties)
            )
            {
                dbProvider.Type = type;
            }

            if (!String.IsNullOrEmpty(dbProvider.Type))
            {
                dbProvider.Factory = DbProviderManager.Instance.GetDbProviderFactory(dbProvider.Type);
            }
            else
            {
                if (!DbProviderManager.Instance.TryInit(ref dbProvider))
                {
                    throw new SmartSqlException("DbProvider can not be Init.");
                }
            }

            return dbProvider;
        }
        private WriteDataSource ParseWriteDataSource(XmlNode writeDataSourceNode)
        {
            if (writeDataSourceNode == null)
            {
                throw new SmartSqlException("WriteDataSource can not be null.");
            }
            var writeDataSource = new WriteDataSource();
            if (writeDataSourceNode.Attributes.TryGetValueAsString(nameof(WriteDataSource.Name), out string name, SmartSqlConfig.Properties)
            )
            {
                writeDataSource.Name = name;
            }
            if (writeDataSourceNode.Attributes.TryGetValueAsString(nameof(WriteDataSource.ConnectionString), out string connectionString, SmartSqlConfig.Properties)
            )
            {
                writeDataSource.ConnectionString = connectionString;
            }
          
            return writeDataSource;
        }
        private IDictionary<string, ReadDataSource> ParseReadDataSource(XmlNodeList readDataSourceNodes, DbProvider dbProvider)
        {
            var readDataSources = new Dictionary<string, ReadDataSource>();
            var s = new List<string>();           
            foreach (XmlNode readDataSourceNode in readDataSourceNodes)
            {  
               
                var readDataSource = new ReadDataSource();
                if (readDataSourceNode.Attributes.TryGetValueAsString(nameof(ReadDataSource.Name), out string name, SmartSqlConfig.Properties)                )
                {
                    readDataSource.Name = name;
                }
                else
                {
                      throw new SmartSqlException($"Read Nodes must have Name attribute");
                }
                if (readDataSourceNode.Attributes.TryGetValueAsString(nameof(ReadDataSource.ConnectionString), out string connectionString, SmartSqlConfig.Properties)                )
                {
                    readDataSource.ConnectionString = connectionString;
                }
                if (readDataSourceNode.Attributes.TryGetValueAsInt32(nameof(ReadDataSource.Weight), out int weight, SmartSqlConfig.Properties)                )
                {
                    readDataSource.Weight = weight;
                }
                else
                {
                    if (readDataSourceNodes.Count > 1)
                    {
                         throw new SmartSqlException($"Read Nodes must have Weight attribute,Name is {readDataSource.Name}");
                    }                  
                }
                readDataSource.DbProvider = dbProvider;
                readDataSources.Add(readDataSource.Name, readDataSource);
            }
            return readDataSources;
        }

        #endregion
        #region 3. TypeHandlers
        protected override void BuildTypeHandlers()
        {
            var typeHandlerXPath = $"{CONFIG_PREFIX}:TypeHandlers/{CONFIG_PREFIX}:TypeHandler";
            var typeHandlerNodes = XmlConfigRoot.SelectNodes(typeHandlerXPath, XmlNsManager);
            if (typeHandlerNodes != null)
            {
                foreach (XmlNode typeHandlerNode in typeHandlerNodes)
                {
                    BuildTypeHandler(typeHandlerNode);
                }
            }
        }
        private void BuildTypeHandler(XmlNode typeHandlerNode)
        {
            TypeHandler typeHandlerConfig = new TypeHandler
            {
                Properties = ParseProperties(typeHandlerNode)
            };
            if (typeHandlerNode.Attributes.TryGetValueAsString(nameof(TypeHandler.Name), out string handlerName, SmartSqlConfig.Properties))
            {
                typeHandlerConfig.Name = handlerName;
            }
            if (!typeHandlerNode.Attributes.TryGetValueAsString(TYPE_ATTRIBUTE, out string type, SmartSqlConfig.Properties))
            {
                throw new SmartSqlException("TypeHandler.Type can not be null.");
            }
            typeHandlerConfig.HandlerType = TypeUtils.GetType(type);

            if (typeHandlerConfig.HandlerType.IsGenericType)
            {
                typeHandlerNode.Attributes.TryGetValueAsString(nameof(TypeHandler.PropertyType), out string propertyTypeStr, SmartSqlConfig.Properties);
                typeHandlerNode.Attributes.TryGetValueAsString(nameof(TypeHandler.FieldType), out string fieldTypeStr, SmartSqlConfig.Properties);

                if (String.IsNullOrEmpty(propertyTypeStr))
                {
                    throw new SmartSqlException("TypeHandler.PropertyType can not be null.");
                }
                typeHandlerConfig.PropertyType = TypeUtils.GetType(propertyTypeStr);
                if (!String.IsNullOrEmpty(fieldTypeStr))
                {
                    typeHandlerConfig.FieldType = TypeUtils.GetType(fieldTypeStr);
                }
            }
            RegisterTypeHandler(typeHandlerConfig);
        }


        #endregion
        #region 4. TagBuilders
        protected override void BuildTagBuilders()
        {
            var tagBuilderXPath = $"{CONFIG_PREFIX}:TagBuilders/{CONFIG_PREFIX}:TagBuilder";
            var tagBuilderNodes = XmlConfigRoot.SelectNodes(tagBuilderXPath, XmlNsManager);
            if (tagBuilderNodes == null) return;
            foreach (XmlNode tagBuilderNode in tagBuilderNodes)
            {
                BuildTagBuilder(tagBuilderNode);
            }
        }
        private void BuildTagBuilder(XmlNode tagBuilderNode)
        {
            if (!tagBuilderNode.Attributes.TryGetValueAsString("Name", out string name, SmartSqlConfig.Properties)
            )
            {
                throw new SmartSqlException("TagBuilder.Name can not be null.");
            }
            if (!tagBuilderNode.Attributes.TryGetValueAsString(TYPE_ATTRIBUTE, out string type, SmartSqlConfig.Properties)
            )
            {
                throw new SmartSqlException("TagBuilder.Type can not be null.");
            }
            RegisterTagBuilder(name, type);
        }


        #endregion
        #region 5. SmartSqlMaps
        protected override void BuildSqlMaps()
        {
            var sqlMapXPath = $"{CONFIG_PREFIX}:SmartSqlMaps/{CONFIG_PREFIX}:SmartSqlMap";
            var sqlMapNodes = XmlConfigRoot.SelectNodes(sqlMapXPath, XmlNsManager);
            if (sqlMapNodes == null) return;
            foreach (XmlNode sqlMapNode in sqlMapNodes)
            {
                BuildSqlMap(sqlMapNode);
            }
        }
        protected void BuildSqlMap(XmlNode sqlMapNode)
        {
            if (!sqlMapNode.Attributes.TryGetValueAsString(nameof(SqlMap.Path), out string path, SmartSqlConfig.Properties)
            )
            {
                throw new SmartSqlException("SmartSqlMap.Path can not be null.");
            }
            if (!sqlMapNode.Attributes.TryGetValueAsString(TYPE_ATTRIBUTE, out string type, SmartSqlConfig.Properties)
            )
            {
                throw new SmartSqlException("SmartSqlMap.Type can not be null.");
            }
            ResourceType resourceType = ResourceType.File;
            Enum.TryParse<ResourceType>(type, out resourceType);
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug($"XmlConfigBuilder BuildSqlMap ->> ResourceType:[{resourceType}] , Path :[{path}] Starting.");
            }
            BuildSqlMap(resourceType, path);
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug($"XmlConfigBuilder BuildSqlMap ->> ResourceType:[{resourceType}] , Path :[{path}] End.");
            }
        }
        #endregion
        #region 6. AutoConverter

        protected override void BuildAutoConverters()
        {
            var authConverterXPath = $"{CONFIG_PREFIX}:AutoConverters/{CONFIG_PREFIX}:AutoConverter";
            var autoConverterNodes = XmlConfigRoot.SelectNodes(authConverterXPath, XmlNsManager);
            if (autoConverterNodes != null)
            {
                foreach (XmlNode autoConverterNode in autoConverterNodes)
                {
                    BuildAutoConverter(autoConverterNode);
                }
            }
        }

        private void BuildAutoConverter(XmlNode autoConverterNode)
        {
            if (!autoConverterNode.Attributes.TryGetValueAsString("Name", out String converterName, SmartSqlConfig.Properties))
            {
                throw new SmartSqlException("AutoConverter.Name can not be null.");
            }

            if (!autoConverterNode.Attributes.TryGetValueAsBoolean("Default", out bool isDefault, SmartSqlConfig.Properties))
            {
                isDefault = false;
            }

            var tokenizerXPath = $"{CONFIG_PREFIX}:Tokenizer";
            var tokenizerNode = autoConverterNode.SelectSingleNode(tokenizerXPath, XmlNsManager);
            if (tokenizerNode == null)
            {
                throw new SmartSqlException("AutoConverter.Tokenizer can not be null");
            }

            var tokenizer = BuildTokenizer(tokenizerNode);

            var wordsConverterXPath = $"{CONFIG_PREFIX}:Converter";
            var wordsConverterNode = autoConverterNode.SelectSingleNode(wordsConverterXPath, XmlNsManager);
            if (wordsConverterNode == null)
            {
                throw new SmartSqlException("AutoConverter.Converter can not be null");
            }

            var wordsConverter = BuildWordsConverter(wordsConverterNode);

            var autoConverter = new AutoConverter.AutoConverter(converterName, tokenizer, wordsConverter);

            SmartSqlConfig.AutoConverters.Add(autoConverter.Name, autoConverter);
            if (isDefault)
            {
                SmartSqlConfig.DefaultAutoConverter = autoConverter;
            }
        }

        private ITokenizer BuildTokenizer(XmlNode tokenizerNode)
        {
            if (!tokenizerNode.Attributes.TryGetValueAsString("Name", out String tokenizerName, SmartSqlConfig.Properties))
            {
                throw new SmartSqlException("Tokenizer.Name can not be null.");
            }
            var properties = ParseProperties(tokenizerNode);
            return _tokenizerBuilder.Build(tokenizerName, properties);
        }

        private IWordsConverter BuildWordsConverter(XmlNode wordsConverterNode)
        {
            if (!wordsConverterNode.Attributes.TryGetValueAsString("Name", out String wordsConverterName, SmartSqlConfig.Properties))
            {
                throw new SmartSqlException("WordsConverter.Name can not be null.");
            }
            var properties = ParseProperties(wordsConverterNode);
            return _wordsConverterBuilder.Build(wordsConverterName, properties);
        }

        #endregion


        private IDictionary<String, object> ParseProperties(XmlNode parentNode)
        {
            var parametersNode = parentNode.SelectSingleNode($"{CONFIG_PREFIX}:Properties", XmlNsManager);
            var parameters = new Dictionary<String, object>();
            if (parametersNode == null) { return parameters; }
            var paramNodes = parametersNode.SelectNodes($"{CONFIG_PREFIX}:Property", XmlNsManager);
            if (paramNodes != null && paramNodes.Count > 0)
            {
                foreach (XmlNode paramterNode in paramNodes)
                {
                    paramterNode.Attributes.TryGetValueAsString("Name", out var name, SmartSqlConfig.Properties);
                    paramterNode.Attributes.TryGetValueAsString("Value", out var propVal, SmartSqlConfig.Properties);
                    parameters.Add(name, propVal);
                }
            }
            return parameters;
        }
    }
}
