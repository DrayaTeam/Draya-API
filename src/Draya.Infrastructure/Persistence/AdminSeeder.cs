using Draya.Domain.Identity;
using Draya.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Draya.Infrastructure.Persistence;

public static class AdminSeeder
{
    public static async Task SeedSuperAdminAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        const string adminEmail = "admin@draya.com";
        const string adminPassword = "Admin@123456";
        const string adminFullName = "Super Admin";
        const string roleName = "SuperAdmin";

        // Ensure SuperAdmin role exists
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid> { Name = roleName, NormalizedName = roleName.ToUpperInvariant() });
            logger.LogInformation("Created '{Role}' role.", roleName);
        }

        // Check if admin user already exists
        var normalizedEmail = adminEmail.ToUpperInvariant();
        var existingUser = System.Linq.Queryable.FirstOrDefault(userManager.Users, u => u.NormalizedEmail == normalizedEmail);
        if (existingUser is not null)
        {
            // Make sure they have the SuperAdmin role
            if (!await userManager.IsInRoleAsync(existingUser, roleName))
            {
                await userManager.AddToRoleAsync(existingUser, roleName);
                logger.LogInformation("Added '{Role}' role to existing user '{Email}'.", roleName, adminEmail);
            }
            return;
        }

        // Create the admin user
        var adminUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            PlatformAdmin = new PlatformAdmin
            {
                FullName = adminFullName
            }
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, roleName);
            logger.LogInformation("Seeded SuperAdmin user: {Email} / {Password}", adminEmail, adminPassword);
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Failed to seed SuperAdmin: {Errors}", errors);
        }

        // Seed default ClassroomTypes and GradeLevels if empty
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (!dbContext.ClassroomTypes.Any())
        {
            dbContext.ClassroomTypes.AddRange(
                new Domain.Classrooms.ClassroomType { Id = Guid.NewGuid(), Name = "Online Group", Description = "Interactive online group class", IsActive = true },
                new Domain.Classrooms.ClassroomType { Id = Guid.NewGuid(), Name = "Private 1-on-1", Description = "One-on-one private tutoring", IsActive = true },
                new Domain.Classrooms.ClassroomType { Id = Guid.NewGuid(), Name = "In-Person Center", Description = "Physical classroom session", IsActive = true }
            );
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Seeded default ClassroomTypes.");
        }

        if (!dbContext.GradeLevels.Any())
        {
            dbContext.GradeLevels.AddRange(
                new Domain.Classrooms.GradeLevel { Id = Guid.NewGuid(), Name = "Primary / Grade 1-6", Description = "Primary education level", SortOrder = 1, IsActive = true },
                new Domain.Classrooms.GradeLevel { Id = Guid.NewGuid(), Name = "Preparatory / Grade 7-9", Description = "Preparatory education level", SortOrder = 2, IsActive = true },
                new Domain.Classrooms.GradeLevel { Id = Guid.NewGuid(), Name = "Secondary / Grade 10-12", Description = "Secondary education level", SortOrder = 3, IsActive = true }
            );
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Seeded default GradeLevels.");
        }
    }
}
