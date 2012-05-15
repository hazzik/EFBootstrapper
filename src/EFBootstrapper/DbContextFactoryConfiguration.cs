namespace Hazzik.EFBootstrapper
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.Entity;
    using System.Data.Entity.ModelConfiguration;
    using System.Data.Entity.ModelConfiguration.Configuration;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    public class DbContextFactoryConfiguration
    {
        private static readonly Type EntityType = typeof (EntityTypeConfiguration<>);

        private static readonly Type ComplexType = typeof (ComplexTypeConfiguration<>);

        private static readonly Lazy<Func<string, DbConnection>> DbConnectionFactoryLazy = new Lazy<Func<string, DbConnection>>(CreateDbConnectionFactory);
        
        private readonly ICollection<Assembly> assemblies = new HashSet<Assembly>();
        private readonly string connectionString;
        private readonly Type contextType;

        private bool autoDetectChangesEnabled;
        private bool lazyLoadingEnabled;
        private bool proxyCreationEnabled;
        private bool validateOnSaveEnabled;

        public DbContextFactoryConfiguration(Type contextType, string connectionString)
        {
            this.contextType = contextType;
            this.connectionString = connectionString;
        }

        private static Func<string, DbConnection> DbConnectionFactory
        {
            get { return DbConnectionFactoryLazy.Value; }
        }

        public DbContextFactoryConfiguration AddMappingsFromAssembly(Assembly assembly)
        {
            assemblies.Add(assembly);
            return this;
        }

        public DbContextFactoryConfiguration AddMappingsFromAssemblyOf(Type type)
        {
            AddMappingsFromAssembly(type.Assembly);
            return this;
        }

        public DbContextFactoryConfiguration AddMappingsFromAssemblyOf<T>()
        {
            AddMappingsFromAssemblyOf(typeof (T));
            return this;
        }

        public DbContextFactoryConfiguration AutoDetectChangesEnabled(bool value)
        {
            autoDetectChangesEnabled = value;
            return this;
        }

        public IDbContextFactory BuildDataContextFactory()
        {
            var builder = new DbModelBuilder();

            var addMethods = typeof (ConfigurationRegistrar).GetMethods()
                .Where(m => m.Name.Equals("Add"))
                .ToArray();

            var entityTypeMethod = addMethods.Single(m => CanCallWithParameterOf(m, EntityType));

            var complexTypeMethod = addMethods.Single(m => CanCallWithParameterOf(m, ComplexType));

            var types = assemblies
                .SelectMany(assembly => assembly.GetExportedTypes())
                .Where(type => type.IsClass && !type.IsAbstract);

            foreach (var type in types)
            {
                Type foundType = null;
                if (IsMatching(type, t => HasGenericTypeDefinition(t, EntityType, out foundType)))
                {
                    var modelType = foundType.GetGenericArguments().First();
                    var typedMethod = entityTypeMethod.MakeGenericMethod(modelType);
                    typedMethod.Invoke(builder.Configurations, new[] {Activator.CreateInstance(type)});
                }
                else if (IsMatching(type, t => HasGenericTypeDefinition(t, ComplexType, out foundType)))
                {
                    var modelType = foundType.GetGenericArguments().First();
                    var typedMethod = complexTypeMethod.MakeGenericMethod(modelType);
                    typedMethod.Invoke(builder.Configurations, new[] {Activator.CreateInstance(type)});
                }
            }

            var connection = DbConnectionFactory(connectionString);

            var model = builder
                .Build(connection)
                .Compile();

            return new DbContextFactory(contextType, connectionString, model)
                       {
                           AutoDetectChangesEnabled = autoDetectChangesEnabled,
                           LazyLoadingEnabled = lazyLoadingEnabled,
                           ProxyCreationEnabled = proxyCreationEnabled,
                           ValidateOnSaveEnabled = validateOnSaveEnabled
                       };
        }

        public DbContextFactoryConfiguration LazyLoadingEnabled(bool value)
        {
            lazyLoadingEnabled = value;
            return this;
        }

        public DbContextFactoryConfiguration ProxyCreationEnabled(bool value)
        {
            proxyCreationEnabled = value;
            return this;
        }

        public DbContextFactoryConfiguration ValidateOnSaveEnabled(bool value)
        {
            validateOnSaveEnabled = value;
            return this;
        }

        private static bool CanCallWithParameterOf(MethodInfo method, Type type)
        {
            return method.GetParameters().First()
                .ParameterType.GetGenericTypeDefinition()
                .IsAssignableFrom(type);
        }

        private static Func<string, DbConnection> CreateDbConnectionFactory()
        {
            var lazyInternalConnectionType = Type.GetType("System.Data.Entity.Internal.LazyInternalConnection, EntityFramework, PublicKeyToken=b77a5c561934e089");
            if (lazyInternalConnectionType == null)
            {
                throw new InvalidOperationException("Could not find 'System.Data.Entity.Internal.LazyInternalConnection' type.");
            }

            var lazyInternalConnectionConstructor = lazyInternalConnectionType.GetConstructor(new[] {typeof (string)});
            if (lazyInternalConnectionConstructor == null)
            {
                throw new InvalidOperationException("Could not find constructor of 'System.Data.Entity.Internal.LazyInternalConnection' type accepting string.");
            }

            var parameter = Expression.Parameter(typeof (string));

            return Expression.Lambda<Func<string, DbConnection>>(
                Expression.PropertyOrField(
                    Expression.New(lazyInternalConnectionConstructor, parameter), "Connection"),
                parameter).Compile();
        }

        private static bool HasGenericTypeDefinition(Type t, Type typeDefinition, out Type foundType)
        {
            foundType = null;
            if (t.IsGenericType && typeDefinition.IsAssignableFrom(t.GetGenericTypeDefinition()))
            {
                foundType = t;
                return true;
            }
            return false;
        }

        private static bool IsMatching(Type matchingType, Predicate<Type> matcher)
        {
            while (matchingType != null)
            {
                if (matcher(matchingType))
                {
                    return true;
                }

                matchingType = matchingType.BaseType;
            }

            return false;
        }
    }
}