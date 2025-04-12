using Keel.Infra.Db.Access;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Keel.Infra.Db.IoC;

public static class DbServices
{
    public static IServiceCollection AddDbLayer<TDbContext>(this IServiceCollection services) 
        where TDbContext : DbContext
    {
        services
            .AddScoped(
                provider => provider.GetRequiredService<IDbLayer<TDbContext>>().CastTo<IDbLayer>())
            .AddScoped<IDbLayer<TDbContext>, DbLayer<TDbContext>>();

        return services;
    }
}