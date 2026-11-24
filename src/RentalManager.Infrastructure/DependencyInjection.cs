using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RentalManager.Application.Interfaces;
using RentalManager.Infrastructure.Persistence;
using RentalManager.Infrastructure.Security;
using RentalManager.Infrastructure.Services;

namespace RentalManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MySql")
            ?? throw new InvalidOperationException("ConnectionStrings:MySql is not configured.");

        services.AddDbContext<RentalDbContext>(options =>
            options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(x => !string.IsNullOrWhiteSpace(x.Key), "Jwt:Key is required.")
            .Validate(x => x.Key.Length >= 32, "Jwt:Key must be at least 32 characters.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.Issuer), "Jwt:Issuer is required.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.Audience), "Jwt:Audience is required.")
            .ValidateOnStart();

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICatalogueService, CatalogueService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<DatabaseInitializer>();

        return services;
    }
}
