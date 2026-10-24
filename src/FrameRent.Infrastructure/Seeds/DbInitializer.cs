using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FrameRent.Domain.Entities;
using FrameRent.Infrastructure.Persistence;

namespace FrameRent.Infrastructure.Seeds;

public static class DbInitializer
{
    public static async Task SeedAsync(
        ApplicationDbContext context, 
        UserManager<IdentityUser> userManager, 
        RoleManager<IdentityRole> roleManager)
    {
        // 1. Roles
        string[] roleNames = { "Staff", "Client" };

        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // 2. Staff user
        var staffEmail = "admin@framerent.com";
        var staffUser = await userManager.FindByEmailAsync(staffEmail);

        if (staffUser == null)
        {
            var newStaff = new IdentityUser
            {
                UserName = staffEmail,
                Email = staffEmail,
                EmailConfirmed = true
            };

            var createStaffResult = await userManager.CreateAsync(newStaff, "StaffSecure2026*");

            if (createStaffResult.Succeeded)
            {
                await userManager.AddToRoleAsync(newStaff, "Staff");
            }
        }

        // 3. Initial Equipment seed if catalog is empty
        if (!await context.Equipments.AnyAsync())
        {
            var initialEquipment = new List<Equipment>
            {
                new("Sony FX3 Cinema Camera", "Cameras", "Full-frame cinema camera with 4K 120p, 10-bit 4:2:2, Dual Base ISO.", 85.00m, 5),
                new("Canon EOS R5 C", "Cameras", "Dual-system 8K mirrorless cinema camera with 45MP full-frame CMOS sensor.", 95.00m, 4),
                new("Sony FE 24-70mm f/2.8 GM II", "Lenses", "Standard zoom G Master lens delivering extreme resolution and bokeh.", 40.00m, 6),
                new("Aputure Light Storm 300d II", "Lighting", "High output 5500K daylight LED fixture with Sidus Link app control.", 35.00m, 8),
                new("Nanlite Forza 60B II Bi-Color", "Lighting", "Ultra-compact bi-color spotlight with refined user interface.", 25.00m, 10),
                new("RØDE Wireless PRO System", "Audio", "Compact wireless microphone system with 32-bit float on-board recording.", 28.00m, 7),
                new("Sennheiser MKH 416 Shotgun Mic", "Audio", "Industry standard professional shotgun interference tube microphone.", 32.00m, 5),
                new("DJI RS 3 Pro Gimbal Stabilizer", "Gimbals & Grip", "Carbon fiber 3-axis stabilizer with automated axis locks and 4.5kg payload.", 45.00m, 6),
                new("Manfrotto 504X Video Tripod", "Gimbals & Grip", "Fluid video head with carbon fiber twin leg system.", 22.00m, 8),
                new("Atomos Ninja V+ 5.2\" Monitor-Recorder", "Monitors & Accessories", "1000nit HDR monitor-recorder capable of up to 8K30p ProRes RAW.", 30.00m, 6)
            };

            await context.Equipments.AddRangeAsync(initialEquipment);
            await context.SaveChangesAsync();
        }
    }
}
