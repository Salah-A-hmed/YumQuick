using Microsoft.AspNetCore.Identity;

namespace YumQuick.Data
{
    public static class DefaultRolesSeeder
    {
        public static async Task SeedAsync(RoleManager<IdentityRole> roleManager)
        {
            if (!await roleManager.RoleExistsAsync("Customer"))
                await roleManager.CreateAsync(new IdentityRole("Customer"));

            if (!await roleManager.RoleExistsAsync("RestaurantManager"))
                await roleManager.CreateAsync(new IdentityRole("RestaurantManager"));

            if (!await roleManager.RoleExistsAsync("DeliveryDriver"))
                await roleManager.CreateAsync(new IdentityRole("DeliveryDriver"));
        }
    }
}