using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OIO.Domain.Context.UserContext.ValueObjects;

namespace OIO.Infrastructure.Persistence.Extensions;

public static class MigrationExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            logger.LogInformation("Applying database migrations...");

            await dbContext.Database.MigrateAsync();
            
            

            logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying database migrations.");
            throw;
        }
    }
}