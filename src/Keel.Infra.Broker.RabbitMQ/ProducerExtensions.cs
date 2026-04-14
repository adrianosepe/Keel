using Keel.Infra.Broker.RabbitMQ;
using MassTransit;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;
// ReSharper restore CheckNamespace

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddKeelProducer<TMessage>(this IServiceCollection services, string queue)
        where TMessage : class
    {
        return services
            .AddScoped<IKeelProducer<TMessage>>(
                sp =>
                    {
                        var provider = sp.GetRequiredService<ISendEndpointProvider>();
                        return new KeelProducer<TMessage>(provider, queue);
                    });
    }
    
    public static void AddKeelConsumer<TConsumer>(this IBusRegistrationConfigurator busRegistrationConfigurator)
        where TConsumer : class, IConsumer
    {
        busRegistrationConfigurator.AddConsumer<TConsumer>(
            ((context, configurator) =>
                {
                    configurator.UseMessageRetry(
                        r => r.Exponential(
                            retryLimit: 5,
                            minInterval: TimeSpan.FromSeconds(1),
                            maxInterval: TimeSpan.FromSeconds(10),
                            intervalDelta: TimeSpan.FromSeconds(2)
                        ));
                }));
    }

    public static void ConfigureKeelConsumer<TConsumer, TMessage>(
        this IBusRegistrationConfigurator busRegistrationConfigurator, 
        string queue, 
        IRabbitMqBusFactoryConfigurator configurator, 
        IBusRegistrationContext context) 
        where TConsumer : KeelConsumer<TMessage> 
        where TMessage : class
    {
        
        configurator.ReceiveEndpoint(queue, e =>
            {
                e.ConfigureConsumer<TConsumer>(context);

                e.UseMessageRetry(
                    r => r.Exponential(
                        retryLimit: 5,
                        minInterval: TimeSpan.FromSeconds(1),
                        maxInterval: TimeSpan.FromSeconds(10),
                        intervalDelta: TimeSpan.FromSeconds(2)
                    ));
            });
    }
}