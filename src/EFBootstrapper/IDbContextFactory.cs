namespace Hazzik.EFBootstrapper
{
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;

    public interface IDbContextFactory : IDbContextFactory<DbContext>
    {
    }
}