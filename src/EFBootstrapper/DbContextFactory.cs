namespace Hazzik.EFBootstrapper
{
    using System;
    using System.Data.Entity;

    internal class DbContextFactory : IDbContextFactory
    {
        private readonly Func<DbContext> factory;

        public DbContextFactory(Func<DbContext> factory)
        {
            this.factory = factory;
        }

        public bool AutoDetectChangesEnabled { get; set; }

        public bool LazyLoadingEnabled { get; set; }

        public bool ValidateOnSaveEnabled { get; set; }

        public bool ProxyCreationEnabled { get; set; }

        public DbContext Create()
        {
            var context = factory();
            context.Configuration.AutoDetectChangesEnabled = AutoDetectChangesEnabled;
            context.Configuration.LazyLoadingEnabled = LazyLoadingEnabled;
            context.Configuration.ProxyCreationEnabled = ProxyCreationEnabled;
            context.Configuration.ValidateOnSaveEnabled = ValidateOnSaveEnabled;
            return context;
        }
    }
}