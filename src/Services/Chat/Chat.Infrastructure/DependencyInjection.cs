using Chat.Application.Contracts;
using Chat.Infrastructure.Persistence;
using Chat.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chat.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ChatWriteDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("ChatWriteDb")));
        services.AddDbContext<ChatReadDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("ChatReadDb")));
        services.AddScoped<IWriteChatRepository, SqlWriteChatRepository>();
        services.AddScoped<IReadChatRepository, SqlReadChatRepository>();

        return services;
    }

    public static async Task EnsureDatabaseCreatedAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChatWriteDbContext>();
        // Use migrations instead of EnsureCreated to keep schema under migrations control
        await dbContext.Database.MigrateAsync();
    }
}
