namespace FrameRent.Application.DTOs.Equipment;

public record EquipmentDto(
    Guid Id,
    string Name,
    string Category,
    string Description,
    decimal DailyRate,
    int TotalUnits,
    bool IsActive
);

public record CreateEquipmentRequest(
    string Name,
    string Category,
    string Description,
    decimal DailyRate,
    int TotalUnits
);

public record UpdateEquipmentRequest(
    string Name,
    string Category,
    string Description,
    decimal DailyRate,
    int TotalUnits
);

public record EquipmentAvailabilityDto(
    Guid EquipmentId,
    string EquipmentName,
    DateTime From,
    DateTime To,
    int TotalUnits,
    int BookedUnits,
    int AvailableUnits
);
