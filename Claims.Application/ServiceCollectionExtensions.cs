using System.Reflection;
using Claims.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Claims.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<IClaimService, ClaimService>();
        services.AddScoped<ICoverService, CoverService>();

        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
