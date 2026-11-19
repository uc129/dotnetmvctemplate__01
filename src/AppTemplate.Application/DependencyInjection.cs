using AppTemplate.Application.Common;
using AppTemplate.Application.Customers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppTemplate.Application;

public static class DependencyInjection
{
    /// <summary>Registers application services. Each layer registers its own types; Program.cs only calls these.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BusinessTimeOptions>(configuration.GetSection(BusinessTimeOptions.SectionName));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<BusinessClock>();

        services.AddScoped<CustomerService>();
        return services;
    }
}
