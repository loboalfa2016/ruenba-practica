using Microsoft.EntityFrameworkCore;
using FrameRent.Application.Common.Interfaces;
using FrameRent.Application.DTOs.Report;
using FrameRent.Application.Interfaces;

namespace FrameRent.Application.Services;

public class ReportService : IReportService
{
    private readonly IApplicationDbContext _context;

    public ReportService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TopEquipmentReportDto>> GetTopEquipmentReportAsync()
    {
        // Agrupa por equipo y ordena de mayor a menor número de alquileres
        var report = await _context.Equipments
            .AsNoTracking()
            .Select(e => new TopEquipmentReportDto(
                e.Id,
                e.Name,
                e.Category,
                e.Rentals.Count(),
                e.Rentals.Sum(r => r.TotalCost + r.OverduePenalty)
            ))
            .OrderByDescending(r => r.TotalRentals)
            .ThenByDescending(r => r.TotalRevenue)
            .ToListAsync();

        return report;
    }
}
