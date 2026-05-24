using BreastCancer.Community.Behaviors;
using BreastCancer.Community.Domain;
using BreastCancer.Community.Events;
using BreastCancer.Community.Options;
using BreastCancer.Community.Services.Implementation;
using BreastCancer.Community.Services.Interface;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Threading.Channels;

namespace BreastCancer.Community;

public static class CommunityModule
{
    public static IServiceCollection AddCommunityModule(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = typeof(DomainEvent).Assembly;

        services.AddOptions<RedisSettings>()
            .Bind(configuration.GetSection(RedisSettings.RedisSettingsKey))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var redisSettings = sp.GetRequiredService<IOptions<RedisSettings>>().Value;
            var options = ConfigurationOptions.Parse(redisSettings.ConnectionString);
            options.AbortOnConnectFail = false;
            options.ConnectRetry = 5;
            options.ReconnectRetryPolicy = new ExponentialRetry(
                deltaBackOffMilliseconds: 1000,
                maxDeltaBackOffMilliseconds: 10000);
            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<ICacheService, RedisCacheService>();

        services.AddSingleton(_ => Channel.CreateBounded<FanoutJob>(new BoundedChannelOptions(5000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        }));
        services.AddHostedService<FanoutWorker>();

        services.AddMediatR(assembly);
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<IPostCreatedEventSink, NoOpPostCreatedEventSink>();
        services.AddScoped<IFollowCreatedEventSink, NoOpFollowCreatedEventSink>();

        return services;
    }
}
