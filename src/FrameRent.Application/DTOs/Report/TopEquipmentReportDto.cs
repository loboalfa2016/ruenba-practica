namespace FrameRent.Application.DTOs.Report;

public record TopEquipmentReportDto(
    Guid EquipmentId,
    string EquipmentName,
    string Category,
    int TotalRentals,
    decimal TotalRevenue
);
