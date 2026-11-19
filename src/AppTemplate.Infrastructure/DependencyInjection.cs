using AppTemplate.Application.Customers;
using AppTemplate.Infrastructure.Customers;
using AppTemplate.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppTemplate.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ValidateOnStart: a missing connection string stops the app at startup with a clear
        // message, instead of failing on the first request that touches the database.
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), "Database:ConnectionString is not set. Use user-secrets locally, environment variables on servers.")
            .ValidateOnStart();

        services.AddScoped<DapperContext>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        return services;
    }
}
