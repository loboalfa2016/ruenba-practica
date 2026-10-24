using Microsoft.EntityFrameworkCore;
using FrameRent.Application.Common.Interfaces;
using FrameRent.Application.DTOs.Equipment;
using FrameRent.Application.Interfaces;
using FrameRent.Domain.Entities;
using FrameRent.Domain.Enums;

namespace FrameRent.Application.Services;

public class EquipmentService : IEquipmentService
{
    private readonly IApplicationDbContext _context;

    public EquipmentService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<EquipmentDto>> GetActiveCatalogAsync()
    {
        return await _context.Equipments
            .AsNoTracking()
            .Where(e => e.IsActive)
            .OrderBy(e => e.Name)
            .Select(e => new EquipmentDto(
                e.Id,
                e.Name,
                e.Category,
                e.Description,
                e.DailyRate,
                e.TotalUnits,
                e.IsActive
            ))
            .ToListAsync();
    }

    public async Task<IEnumerable<EquipmentDto>> GetAllAsync()
    {
        return await _context.Equipments
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .Select(e => new EquipmentDto(
                e.Id,
                e.Name,
                e.Category,
                e.Description,
                e.DailyRate,
                e.TotalUnits,
                e.IsActive
            ))
            .ToListAsync();
    }

    public async Task<EquipmentDto> GetByIdAsync(Guid id)
    {
        var equipment = await _context.Equipments
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new KeyNotFoundException($"Equipment with ID '{id}' was not found.");

        return new EquipmentDto(
            equipment.Id,
            equipment.Name,
            equipment.Category,
            equipment.Description,
            equipment.DailyRate,
            equipment.TotalUnits,
            equipment.IsActive
        );
    }

    public async Task<EquipmentAvailabilityDto> GetAvailabilityAsync(Guid id, DateTime from, DateTime to)
    {
        var equipment = await _context.Equipments
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new KeyNotFoundException($"Equipment with ID '{id}' was not found.");

        var fromDate = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);
        var toDate = DateTime.SpecifyKind(to.Date, DateTimeKind.Utc);

        if (fromDate < DateTime.UtcNow.Date)
            throw new ArgumentException("Start date cannot be in the past.");

        if (toDate <= fromDate)
            throw new ArgumentException("End date must be after the start date.");

        int rentalDays = (toDate - fromDate).Days;
        if (rentalDays > 15)
            throw new ArgumentException("A rental can last a maximum of 15 days.");

        // Rule 2: Disponibilidad. Se descuentan las unidades de los alquileres aprobados o entregados que se crucen con esas fechas.
        var bookedUnits = await _context.Rentals
            .AsNoTracking()
            .Where(r => r.EquipmentId == id &&
                        (r.Status == RentalStatus.Approved || r.Status == RentalStatus.Delivered) &&
                        r.StartDate <= toDate &&
                        r.EndDate >= fromDate)
            .SumAsync(r => r.Quantity);

        var availableUnits = Math.Max(0, equipment.TotalUnits - bookedUnits);

        return new EquipmentAvailabilityDto(
            equipment.Id,
            equipment.Name,
            fromDate,
            toDate,
            equipment.TotalUnits,
            bookedUnits,
            availableUnits
        );
    }

    public async Task<EquipmentDto> CreateAsync(CreateEquipmentRequest request)
    {
        var equipment = new Equipment(
            request.Name,
            request.Category,
            request.Description,
            request.DailyRate,
            request.TotalUnits
        );

        _context.Equipments.Add(equipment);
        await _context.SaveChangesAsync();

        return new EquipmentDto(
            equipment.Id,
            equipment.Name,
            equipment.Category,
            equipment.Description,
            equipment.DailyRate,
            equipment.TotalUnits,
            equipment.IsActive
        );
    }

    public async Task<EquipmentDto> UpdateAsync(Guid id, UpdateEquipmentRequest request)
    {
        var equipment = await _context.Equipments.FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new KeyNotFoundException($"Equipment with ID '{id}' was not found.");

        equipment.Update(
            request.Name,
            request.Category,
            request.Description,
            request.DailyRate,
            request.TotalUnits
        );

        await _context.SaveChangesAsync();

        return new EquipmentDto(
            equipment.Id,
            equipment.Name,
            equipment.Category,
            equipment.Description,
            equipment.DailyRate,
            equipment.TotalUnits,
            equipment.IsActive
        );
    }

    public async Task DeactivateAsync(Guid id)
    {
        var equipment = await _context.Equipments
            .Include(e => e.Rentals)
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new KeyNotFoundException($"Equipment with ID '{id}' was not found.");

        // Domain method validates no approved or delivered rentals exist
        equipment.Deactivate();

        await _context.SaveChangesAsync();
    }

    public async Task ActivateAsync(Guid id)
    {
        var equipment = await _context.Equipments.FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new KeyNotFoundException($"Equipment with ID '{id}' was not found.");

        equipment.Activate();
        await _context.SaveChangesAsync();
    }
}
