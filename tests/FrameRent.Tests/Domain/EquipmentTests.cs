using FrameRent.Domain.Entities;
using Xunit;

namespace FrameRent.Tests.Domain;

public class EquipmentTests
{
    [Fact]
    public void Constructor_ValidArguments_CreatesActiveEquipment()
    {
        var equipment = new Equipment("Sony A7 IV", "Cameras", "Mirrorless hybrid camera", 65m, 4);

        Assert.NotEqual(Guid.Empty, equipment.Id);
        Assert.Equal("Sony A7 IV", equipment.Name);
        Assert.True(equipment.IsActive);
        Assert.Equal(4, equipment.TotalUnits);
    }

    [Theory]
    [InlineData("", "Cameras", "Desc", 50, 2)]
    [InlineData("Canon R5", "Cameras", "Desc", 0, 2)]
    [InlineData("Canon R5", "Cameras", "Desc", -10, 2)]
    [InlineData("Canon R5", "Cameras", "Desc", 50, -1)]
    public void Update_InvalidArguments_ThrowsArgumentException(string name, string category, string desc, decimal rate, int units)
    {
        var equipment = new Equipment("Sony A7 IV", "Cameras", "Description", 65m, 4);

        Assert.Throws<ArgumentException>(() => equipment.Update(name, category, desc, rate, units));
    }

    [Fact]
    public void Deactivate_WhenNoApprovedOrDeliveredRentals_DeactivatesEquipment()
    {
        var equipment = new Equipment("Sony A7 IV", "Cameras", "Description", 65m, 4);

        equipment.Deactivate();

        Assert.False(equipment.IsActive);
    }

    [Fact]
    public void Deactivate_WhenApprovedRentalExists_ThrowsInvalidOperationException()
    {
        var equipment = new Equipment("Sony A7 IV", "Cameras", "Description", 65m, 4);
        var rental = new Rental(equipment.Id, "client-1", 1, DateTime.UtcNow.Date.AddDays(1), DateTime.UtcNow.Date.AddDays(3), 65m);
        rental.Approve();

        equipment.Rentals.Add(rental);

        var ex = Assert.Throws<InvalidOperationException>(() => equipment.Deactivate());
        Assert.Contains("Cannot deactivate equipment with approved or delivered rentals", ex.Message);
    }

    [Fact]
    public void Deactivate_WhenDeliveredRentalExists_ThrowsInvalidOperationException()
    {
        var equipment = new Equipment("Sony A7 IV", "Cameras", "Description", 65m, 4);
        var rental = new Rental(equipment.Id, "client-1", 1, DateTime.UtcNow.Date.AddDays(1), DateTime.UtcNow.Date.AddDays(3), 65m);
        rental.Approve();
        rental.Deliver();

        equipment.Rentals.Add(rental);

        var ex = Assert.Throws<InvalidOperationException>(() => equipment.Deactivate());
        Assert.Contains("Cannot deactivate equipment with approved or delivered rentals", ex.Message);
    }
}
