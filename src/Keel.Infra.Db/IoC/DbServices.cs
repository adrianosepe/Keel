using Keel.Infra.Db.Access;
using Keel.Infra.Db.Orm;
using Keel.Infra.Db.Orm.Transaction;
using Microsoft.Extensions.DependencyInjection;

namespace Keel.Infra.Db.IoC;

public static class DbServices
{
    public static IServiceCollection AddDbLayer<TDbContext, TDbAccess>(this IServiceCollection services) 
        where TDbContext : BaseDbContext where TDbAccess : class, IDbLayer<TDbContext>
    {
        services
            .AddScoped(
                provider => provider.GetRequiredService<IDbLayer<TDbContext>>().CastTo<IDbLayer>())
            .AddScoped<IDbLayer<TDbContext>, TDbAccess>()
            .AddScoped<IDbUnitOfWork, TDbContext>();

        return services;
    }
}