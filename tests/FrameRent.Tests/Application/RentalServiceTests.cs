using Microsoft.EntityFrameworkCore;
using FrameRent.Application.DTOs.Rental;
using FrameRent.Application.Services;
using FrameRent.Domain.Entities;
using FrameRent.Domain.Enums;
using FrameRent.Infrastructure.Persistence;
using Xunit;

namespace FrameRent.Tests.Application;

public class RentalServiceTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateRental_WhenEquipmentIsInactive_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        var equipment = new Equipment("Sony A7", "Cameras", "Desc", 50m, 5);
        equipment.Deactivate();
        context.Equipments.Add(equipment);
        await context.SaveChangesAsync();

        var service = new RentalService(context);
        var request = new CreateRentalRequest(equipment.Id, 1, DateTime.UtcNow.Date.AddDays(1), DateTime.UtcNow.Date.AddDays(3));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateRentalAsync("client-1", request));

        Assert.Contains("Cannot rent an inactive equipment", ex.Message);
    }

    [Fact]
    public async Task CreateRental_WhenCustomerExceeds3ActiveRentals_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        var equipment = new Equipment("Sony A7", "Cameras", "Desc", 50m, 10);
        context.Equipments.Add(equipment);

        string clientId = "client-limit-test";
        // Seed 3 pending/approved rentals
        context.Rentals.AddRange(
            new Rental(equipment.Id, clientId, 1, DateTime.UtcNow.Date.AddDays(1), DateTime.UtcNow.Date.AddDays(2), 50m),
            new Rental(equipment.Id, clientId, 1, DateTime.UtcNow.Date.AddDays(3), DateTime.UtcNow.Date.AddDays(4), 50m),
            new Rental(equipment.Id, clientId, 1, DateTime.UtcNow.Date.AddDays(5), DateTime.UtcNow.Date.AddDays(6), 50m)
        );
        await context.SaveChangesAsync();

        var service = new RentalService(context);
        var request = new CreateRentalRequest(equipment.Id, 1, DateTime.UtcNow.Date.AddDays(7), DateTime.UtcNow.Date.AddDays(8));

        // Rule 4: Client cannot have more than 3 pending or approved rentals
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateRentalAsync(clientId, request));

        Assert.Contains("Customer cannot have more than 3 pending or approved rentals", ex.Message);
    }

    [Fact]
    public async Task CreateRental_WhenUnitsExhaustedDueToOverlappingApprovedRentals_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        var equipment = new Equipment("RED Komodo", "Cameras", "Desc", 150m, 2); // 2 total units
        context.Equipments.Add(equipment);

        // Another client has booked 2 units with Approved status for dates 2 to 6
        var existingRental = new Rental(equipment.Id, "client-other", 2, DateTime.UtcNow.Date.AddDays(2), DateTime.UtcNow.Date.AddDays(6), 150m);
        existingRental.Approve();
        context.Rentals.Add(existingRental);
        await context.SaveChangesAsync();

        var service = new RentalService(context);
        // Request overlapping dates: 3 to 5
        var request = new CreateRentalRequest(equipment.Id, 1, DateTime.UtcNow.Date.AddDays(3), DateTime.UtcNow.Date.AddDays(5));

        // Rule 2: Disponibilidad
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateRentalAsync("client-new", request));

        Assert.Contains("Not enough units available", ex.Message);
    }

    [Fact]
    public async Task ApproveRental_WhenAvailabilityDecreasedSinceRequest_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryDbContext();
        var equipment = new Equipment("Blackmagic 6K", "Cameras", "Desc", 70m, 1); // Only 1 total unit
        context.Equipments.Add(equipment);

        var rentalA = new Rental(equipment.Id, "client-A", 1, DateTime.UtcNow.Date.AddDays(2), DateTime.UtcNow.Date.AddDays(5), 70m);
        var rentalB = new Rental(equipment.Id, "client-B", 1, DateTime.UtcNow.Date.AddDays(2), DateTime.UtcNow.Date.AddDays(5), 70m);
        context.Rentals.AddRange(rentalA, rentalB);
        await context.SaveChangesAsync();

        var service = new RentalService(context);

        // Approve rental A first
        await service.ApproveRentalAsync(rentalA.Id);

        // Rule 6: Re-checks availability on approval. Rental B should now fail because all units are taken.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ApproveRentalAsync(rentalB.Id));

        Assert.Contains("Cannot approve rental: insufficient equipment units available", ex.Message);
    }
}
