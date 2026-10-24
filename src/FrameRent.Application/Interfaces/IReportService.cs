using FrameRent.Application.DTOs.Report;

namespace FrameRent.Application.Interfaces;

public interface IReportService
{
    Task<IEnumerable<TopEquipmentReportDto>> GetTopEquipmentReportAsync();
}
