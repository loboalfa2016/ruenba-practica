using FrameRent.Domain.Entities;
using FrameRent.Domain.Enums;
using Xunit;

namespace FrameRent.Tests.Domain;

public class RentalTests
{
    private readonly Guid _sampleEquipmentId = Guid.NewGuid();
    private readonly string _sampleClientId = "client-123";
    private readonly decimal _dailyRate = 50.00m;

    [Fact]
    public void Constructor_StartDateInPast_ThrowsArgumentException()
    {
        var pastStartDate = DateTime.UtcNow.Date.AddDays(-1);
        var endDate = DateTime.UtcNow.Date.AddDays(2);

        var ex = Assert.Throws<ArgumentException>(() =>
            new Rental(_sampleEquipmentId, _sampleClientId, 1, pastStartDate, endDate, _dailyRate));

        Assert.Contains("Start date cannot be in the past", ex.Message);
    }

    [Fact]
    public void Constructor_EndDateBeforeOrEqualToStartDate_ThrowsArgumentException()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(2);
        var invalidEndDate = startDate; // Same date -> 0 days

        var ex = Assert.Throws<ArgumentException>(() =>
            new Rental(_sampleEquipmentId, _sampleClientId, 1, startDate, invalidEndDate, _dailyRate));

        Assert.Contains("End date must be after the start date", ex.Message);
    }

    [Fact]
    public void Constructor_DurationOver15Days_ThrowsArgumentException()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(1);
        var endDate = startDate.AddDays(16); // 16 days > 15 days limit

        var ex = Assert.Throws<ArgumentException>(() =>
            new Rental(_sampleEquipmentId, _sampleClientId, 1, startDate, endDate, _dailyRate));

        Assert.Contains("maximum of 15 days", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ZeroOrNegativeQuantity_ThrowsArgumentException(int invalidQuantity)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(1);
        var endDate = startDate.AddDays(3);

        var ex = Assert.Throws<ArgumentException>(() =>
            new Rental(_sampleEquipmentId, _sampleClientId, invalidQuantity, startDate, endDate, _dailyRate));

        Assert.Contains("Quantity must be greater than zero", ex.Message);
    }

    [Fact]
    public void Constructor_ValidArguments_CalculatesTotalCostCorrectlyAndSetsStatusPending()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(2);
        var endDate = startDate.AddDays(4); // 4 days
        int quantity = 3;
        decimal rate = 60m;
        decimal expectedCost = 4 * rate * quantity; // 4 * 60 * 3 = 720

        var rental = new Rental(_sampleEquipmentId, _sampleClientId, quantity, startDate, endDate, rate);

        Assert.Equal(RentalStatus.Pending, rental.Status);
        Assert.Equal(expectedCost, rental.TotalCost);
        Assert.Equal(0m, rental.OverduePenalty);
    }

    [Fact]
    public void Approve_WhenPending_SetsStatusToApproved()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(2);
        var rental = new Rental(_sampleEquipmentId, _sampleClientId, 1, startDate, startDate.AddDays(2), _dailyRate);

        rental.Approve();

        Assert.Equal(RentalStatus.Approved, rental.Status);
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_ThrowsInvalidOperationException()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(2);
        var rental = new Rental(_sampleEquipmentId, _sampleClientId, 1, startDate, startDate.AddDays(2), _dailyRate);
        rental.Approve();

        Assert.Throws<InvalidOperationException>(() => rental.Approve());
    }

    [Fact]
    public void Reject_WhenPending_SetsStatusToRejected()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(2);
        var rental = new Rental(_sampleEquipmentId, _sampleClientId, 1, startDate, startDate.AddDays(2), _dailyRate);

        rental.Reject();

        Assert.Equal(RentalStatus.Rejected, rental.Status);
    }

    [Fact]
    public void Deliver_WhenApproved_SetsStatusToDelivered()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(2);
        var rental = new Rental(_sampleEquipmentId, _sampleClientId, 1, startDate, startDate.AddDays(2), _dailyRate);
        rental.Approve();

        rental.Deliver();

        Assert.Equal(RentalStatus.Delivered, rental.Status);
    }

    [Fact]
    public void Deliver_WhenNotApproved_ThrowsInvalidOperationException()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(2);
        var rental = new Rental(_sampleEquipmentId, _sampleClientId, 1, startDate, startDate.AddDays(2), _dailyRate);

        Assert.Throws<InvalidOperationException>(() => rental.Deliver());
    }

    [Fact]
    public void Return_OnTime_SetsStatusReturnedAndZeroPenalty()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(1);
        var endDate = startDate.AddDays(3);
        var rental = new Rental(_sampleEquipmentId, _sampleClientId, 2, startDate, endDate, _dailyRate);
        rental.Approve();
        rental.Deliver();

        rental.Return(endDate, _dailyRate);

        Assert.Equal(RentalStatus.Returned, rental.Status);
        Assert.Equal(0m, rental.OverduePenalty);
    }

    [Fact]
    public void Return_Overdue_CalculatesPenaltyAccordingToBusinessFormula()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(1);
        var endDate = startDate.AddDays(3);
        int quantity = 2;
        decimal rate = 50m;
        var rental = new Rental(_sampleEquipmentId, _sampleClientId, quantity, startDate, endDate, rate);
        rental.Approve();
        rental.Deliver();

        // 3 days overdue: formula is overdueDays * dailyRate * quantity * 1.5
        var lateReturnDate = endDate.AddDays(3);
        decimal expectedPenalty = 3 * rate * quantity * 1.5m; // 3 * 50 * 2 * 1.5 = 450

        rental.Return(lateReturnDate, rate);

        Assert.Equal(RentalStatus.Returned, rental.Status);
        Assert.Equal(expectedPenalty, rental.OverduePenalty);
    }

    [Fact]
    public void Cancel_WhenPendingBeforeStartDate_SetsStatusToCanceled()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(3);
        var rental = new Rental(_sampleEquipmentId, _sampleClientId, 1, startDate, startDate.AddDays(2), _dailyRate);

        rental.Cancel();

        Assert.Equal(RentalStatus.Canceled, rental.Status);
    }

    [Fact]
    public void Cancel_WhenApprovedBeforeStartDate_SetsStatusToCanceled()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(3);
        var rental = new Rental(_sampleEquipmentId, _sampleClientId, 1, startDate, startDate.AddDays(2), _dailyRate);
        rental.Approve();

        rental.Cancel();

        Assert.Equal(RentalStatus.Canceled, rental.Status);
    }

    [Fact]
    public void Cancel_WhenStartDateIsToday_ThrowsInvalidOperationException()
    {
        // Rental starting today
        var startDate = DateTime.UtcNow.Date;
        var rental = new Rental(_sampleEquipmentId, _sampleClientId, 1, startDate, startDate.AddDays(2), _dailyRate);

        var ex = Assert.Throws<InvalidOperationException>(() => rental.Cancel());
        Assert.Contains("Cannot cancel a rental on or after its start date", ex.Message);
    }
}
