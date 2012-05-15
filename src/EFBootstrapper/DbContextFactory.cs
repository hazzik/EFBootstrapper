namespace Hazzik.EFBootstrapper
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;

    internal class DbContextFactory : IDbContextFactory
    {
        private readonly string connectionString;
        private readonly Type dataContextType;
        private readonly DbCompiledModel model;

        public DbContextFactory(Type dataContextType, string connectionString, DbCompiledModel model)
        {
            this.dataContextType = dataContextType;
            this.connectionString = connectionString;
            this.model = model;
        }

        public bool AutoDetectChangesEnabled { get; set; }

        public bool LazyLoadingEnabled { get; set; }

        public bool ValidateOnSaveEnabled { get; set; }

        public bool ProxyCreationEnabled { get; set; }

        public DbContext Create()
        {
            var context = (DbContext) Activator.CreateInstance(dataContextType, connectionString, model);
            context.Configuration.AutoDetectChangesEnabled = AutoDetectChangesEnabled;
            context.Configuration.LazyLoadingEnabled = LazyLoadingEnabled;
            context.Configuration.ProxyCreationEnabled = ProxyCreationEnabled;
            context.Configuration.ValidateOnSaveEnabled = ValidateOnSaveEnabled;
            return context;
        }
    }
}