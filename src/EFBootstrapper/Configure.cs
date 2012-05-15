namespace Hazzik.EFBootstrapper
{
    using System;
    using System.Data.Entity;

    public static class Configure
    {
        public static DbContextFactoryConfiguration WithContext<TContext>(string connectionString)
            where TContext : DbContext
        {
            return WithContext(typeof (TContext), connectionString);
        }

        public static DbContextFactoryConfiguration WithContext(string connectionString)
        {
            return WithContext(typeof (DbContext), connectionString);
        }

        public static DbContextFactoryConfiguration WithContext(Type contextType, string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentException("Connection string could not be empty", "connectionString");

            if (!typeof(DbContext).IsAssignableFrom(contextType))
                throw new ArgumentException("Context type is invalid", "contextType");

            return new DbContextFactoryConfiguration(contextType, connectionString);
        }
    }
}