using Microsoft.AspNetCore.Identity;
using IdentityService.Entities;
using System.Linq;

namespace IdentityService.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await EnsureRolesAsync(roleManager);

            await EnsureUserAsync(userManager, "admin", "Sistem Yöneticisi", "Admin", "admin@library.local", "Admin123!", "admin");
            await EnsureUserAsync(userManager, "123456", "Test Öğrenci", "Lisans", "ogrenci@school.edu", "Password123!", "student");
            await EnsureUserAsync(userManager, "turnstile-bot", "Turnstile Servis", "Service", "turnstile@library.local", "Turnstile123!", "service");
        }

        private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            var roles = new[] { "admin", "student", "service" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(role));
                    if (!result.Succeeded)
                    {
                        throw new InvalidOperationException($"Rol oluşturulamadı: {role}. {string.Join(',', result.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }

        private static async Task EnsureUserAsync(UserManager<AppUser> userManager, string studentNumber, string fullName, string academicLevel, string email, string password, string role)
        {
            var normalizedStudentNumber = studentNumber.Trim();
            var user = await userManager.FindByNameAsync(normalizedStudentNumber);
            if (user == null)
            {
                user = new AppUser
                {
                    UserName = normalizedStudentNumber,
                    StudentNumber = normalizedStudentNumber,
                    FullName = fullName,
                    AcademicLevel = academicLevel,
                    Email = email,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException($"Kullanıcı oluşturulamadı ({studentNumber}): {string.Join(',', createResult.Errors.Select(e => e.Description))}");
                }
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                var addRoleResult = await userManager.AddToRoleAsync(user, role);
                if (!addRoleResult.Succeeded)
                {
                    throw new InvalidOperationException($"Kullanıcı role eklenemedi ({studentNumber} -> {role}): {string.Join(',', addRoleResult.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}
