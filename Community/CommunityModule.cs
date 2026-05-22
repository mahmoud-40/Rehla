using BreastCancer.Community.Behaviors;
using BreastCancer.Community.Domain;
using BreastCancer.Community.Events;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace BreastCancer.Community;

public static class CommunityModule
{
    public static IServiceCollection AddCommunityModule(this IServiceCollection services)
    {
        var assembly = typeof(DomainEvent).Assembly;

        services.AddMediatR(assembly);
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<IPostCreatedEventSink, NoOpPostCreatedEventSink>();
        services.AddScoped<IFollowCreatedEventSink, NoOpFollowCreatedEventSink>();

        return services;
    }
}
