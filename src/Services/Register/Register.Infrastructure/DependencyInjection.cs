using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Register.Application.Contracts;
using Register.Infrastructure.Persistence;
using Register.Infrastructure.Repositories;

namespace Register.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<RegisterDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("RegisterDb")));

        services.AddScoped<IUserRegistrationRepository, SqlUserRegistrationRepository>();
        return services;
    }

    public static async Task EnsureDatabaseCreatedAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RegisterDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }
}
