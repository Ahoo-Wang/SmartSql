﻿using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using SmartSql.Abstractions;
using SmartSql.DIExtension;
using SmartSql.DyRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DyRepositoryExtensions
    {
        public static void AddRepositoryFactory(this IServiceCollection services, string scope_template = "", Func<Type, MethodInfo, String> sqlIdNamingConvert = null)
        {
            services.AddSmartSql();
            services.AddSingleton<IRepositoryBuilder>((sp) =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<RepositoryBuilder>();
                return new RepositoryBuilder(scope_template, sqlIdNamingConvert, logger);
            });
            services.AddSingleton<IRepositoryFactory>((sp) =>
            {
                var repositoryBuilder = sp.GetRequiredService<IRepositoryBuilder>();
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<RepositoryFactory>();
                return new RepositoryFactory(repositoryBuilder, logger);
            });
        }
        public static void AddSmartSqlRepositoryFactory(this IServiceCollection services, string scope_template = "", Func<Type, MethodInfo, String> sqlIdNamingConvert = null)
        {
            services.AddRepositoryFactory(scope_template, sqlIdNamingConvert);
        }
        public static void AddRepository<T>(this IServiceCollection services, Func<IServiceProvider, ISmartSqlMapper> getSmartSql = null, string scope = "") where T : class
        {
            services.AddRepositoryFactory();
            services.AddSingleton<T>(sp =>
            {
                ISmartSqlMapper sqlMapper = null;
                if (getSmartSql != null)
                {
                    sqlMapper = getSmartSql(sp);
                }
                sqlMapper = sqlMapper ?? sp.GetRequiredService<ISmartSqlMapper>();
                var factory = sp.GetRequiredService<IRepositoryFactory>();
                return factory.CreateInstance<T>(sqlMapper, scope);
            });
        }
        public static void AddSmartSqlRepository<T>(this IServiceCollection services, Func<IServiceProvider, ISmartSqlMapper> getSmartSql = null, string scope = "") where T : class
        {
            services.AddRepository<T>(getSmartSql, scope);
        }
        public static void AddRepositoryFromAssembly(this IServiceCollection services, Action<AssemblyAutoRegisterOptions> setupOptions)
        {
            services.AddRepositoryFactory();
            var options = new AssemblyAutoRegisterOptions
            {
                Filter = (type) => { return type.IsInterface; }
            };
            setupOptions(options);
            ScopeTemplateParser templateParser = new ScopeTemplateParser(options.ScopeTemplate);
            var assembly = Assembly.Load(options.AssemblyString);
            var allTypes = assembly.GetTypes().Where(options.Filter);
            ISmartSqlMapper sqlMapper = null;
            foreach (var type in allTypes)
            {
                services.AddSingleton(type, sp =>
                {
                    if (sqlMapper == null && options.GetSmartSql != null)
                    {
                        sqlMapper = options.GetSmartSql(sp);
                    }
                    sqlMapper = sqlMapper ?? sp.GetRequiredService<ISmartSqlMapper>();
                    var factory = sp.GetRequiredService<IRepositoryFactory>();
                    var scope = string.Empty;
                    if (!String.IsNullOrEmpty(options.ScopeTemplate))
                    {
                        scope = templateParser.Parse(type.Name);
                    }
                    return factory.CreateInstance(type, sqlMapper, scope);
                });
            }
        }

        public static void AddSmartSqlRepositoryFromAssembly(this IServiceCollection services, Action<AssemblyAutoRegisterOptions> setupOptions)
        {
            services.AddRepositoryFromAssembly(setupOptions);
        }

    }

}
