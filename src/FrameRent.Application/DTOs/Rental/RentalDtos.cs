using FrameRent.Domain.Enums;

namespace FrameRent.Application.DTOs.Rental;

public record CreateRentalRequest(
    Guid EquipmentId,
    int Quantity,
    DateTime StartDate,
    DateTime EndDate
);

public record RentalDto(
    Guid Id,
    Guid EquipmentId,
    string EquipmentName,
    string ClientId,
    int Quantity,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalCost,
    RentalStatus Status,
    decimal OverduePenalty,
    decimal DailyRate
);
