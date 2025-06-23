using Keel.Infra.Db.Orm;
using Keel.Infra.Db.Orm.Transaction;
using Microsoft.Extensions.DependencyInjection;

namespace Keel.Infra.Db.IoC;

public static class DbServices
{
    public static IServiceCollection AddDbLayer<TDbContext, TDbLayer>(this IServiceCollection services) 
        where TDbContext : BaseDbContext where TDbLayer : class, IDbLayer<TDbContext>
    {
        services
            .AddScoped(
                provider => provider.GetRequiredService<IDbLayer<TDbContext>>().CastTo<IDbLayer>())
            .AddScoped<IDbLayer<TDbContext>, TDbLayer>()
            .AddScoped<IDbUnitOfWork, TDbContext>();

        return services;
    }
}