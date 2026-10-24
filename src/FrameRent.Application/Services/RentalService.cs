using Microsoft.EntityFrameworkCore;
using FrameRent.Application.Common.Interfaces;
using FrameRent.Application.DTOs.Rental;
using FrameRent.Application.Interfaces;
using FrameRent.Domain.Entities;
using FrameRent.Domain.Enums;

namespace FrameRent.Application.Services;

public class RentalService : IRentalService
{
    private readonly IApplicationDbContext _context;

    public RentalService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RentalDto> CreateRentalAsync(string clientId, CreateRentalRequest request)
    {
        var equipment = await _context.Equipments.FirstOrDefaultAsync(e => e.Id == request.EquipmentId)
            ?? throw new KeyNotFoundException($"Equipment with ID '{request.EquipmentId}' was not found.");

        if (!equipment.IsActive)
            throw new InvalidOperationException("Cannot rent an inactive equipment.");

        var fromDate = DateTime.SpecifyKind(request.StartDate.Date, DateTimeKind.Utc);
        var toDate = DateTime.SpecifyKind(request.EndDate.Date, DateTimeKind.Utc);

        // Rule 4: Límite por cliente. Un cliente no puede tener más de 3 alquileres pendientes o aprobados al mismo tiempo.
        var activeRentalsCount = await _context.Rentals
            .CountAsync(r => r.ClientId == clientId &&
                             (r.Status == RentalStatus.Pending || r.Status == RentalStatus.Approved));

        if (activeRentalsCount >= 3)
        {
            throw new InvalidOperationException("Customer cannot have more than 3 pending or approved rentals at the same time.");
        }

        // Rule 2: Disponibilidad. No se pueden alquilar más unidades de las disponibles en ese rango de fechas.
        var bookedUnits = await _context.Rentals
            .AsNoTracking()
            .Where(r => r.EquipmentId == request.EquipmentId &&
                        (r.Status == RentalStatus.Approved || r.Status == RentalStatus.Delivered) &&
                        r.StartDate <= toDate &&
                        r.EndDate >= fromDate)
            .SumAsync(r => r.Quantity);

        var availableUnits = equipment.TotalUnits - bookedUnits;
        if (availableUnits < request.Quantity)
        {
            throw new InvalidOperationException($"Not enough units available for the selected dates. Available: {availableUnits}, requested: {request.Quantity}.");
        }

        // Rule 1 & 3: Creates rental entity (calculates total cost, validates dates and duration)
        var rental = new Rental(
            equipment.Id,
            clientId,
            request.Quantity,
            fromDate,
            toDate,
            equipment.DailyRate
        );

        _context.Rentals.Add(rental);
        await _context.SaveChangesAsync();

        return new RentalDto(
            rental.Id,
            rental.EquipmentId,
            equipment.Name,
            rental.ClientId,
            rental.Quantity,
            rental.StartDate,
            rental.EndDate,
            rental.TotalCost,
            rental.Status,
            rental.OverduePenalty,
            equipment.DailyRate
        );
    }

    public async Task<IEnumerable<RentalDto>> GetCustomerRentalsAsync(string clientId)
    {
        return await _context.Rentals
            .AsNoTracking()
            .Include(r => r.Equipment)
            .Where(r => r.ClientId == clientId)
            .OrderByDescending(r => r.StartDate)
            .Select(r => new RentalDto(
                r.Id,
                r.EquipmentId,
                r.Equipment.Name,
                r.ClientId,
                r.Quantity,
                r.StartDate,
                r.EndDate,
                r.TotalCost,
                r.Status,
                r.OverduePenalty,
                r.Equipment.DailyRate
            ))
            .ToListAsync();
    }

    public async Task<RentalDto> GetRentalByIdAsync(Guid id, string userId, bool isStaff)
    {
        var rental = await _context.Rentals
            .AsNoTracking()
            .Include(r => r.Equipment)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException($"Rental with ID '{id}' was not found.");

        if (!isStaff && rental.ClientId != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to view this rental.");
        }

        return new RentalDto(
            rental.Id,
            rental.EquipmentId,
            rental.Equipment.Name,
            rental.ClientId,
            rental.Quantity,
            rental.StartDate,
            rental.EndDate,
            rental.TotalCost,
            rental.Status,
            rental.OverduePenalty,
            rental.Equipment.DailyRate
        );
    }

    public async Task CancelRentalAsync(Guid id, string userId, bool isStaff)
    {
        var rental = await _context.Rentals.FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException($"Rental with ID '{id}' was not found.");

        if (!isStaff && rental.ClientId != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to cancel this rental.");
        }

        // Domain method validates Pending/Approved status and before start date
        rental.Cancel();

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<RentalDto>> GetAllRentalsAsync(RentalStatus? status = null)
    {
        var query = _context.Rentals
            .AsNoTracking()
            .Include(r => r.Equipment)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return await query
            .OrderByDescending(r => r.StartDate)
            .Select(r => new RentalDto(
                r.Id,
                r.EquipmentId,
                r.Equipment.Name,
                r.ClientId,
                r.Quantity,
                r.StartDate,
                r.EndDate,
                r.TotalCost,
                r.Status,
                r.OverduePenalty,
                r.Equipment.DailyRate
            ))
            .ToListAsync();
    }

    public async Task<RentalDto> ApproveRentalAsync(Guid id)
    {
        var rental = await _context.Rentals
            .Include(r => r.Equipment)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException($"Rental with ID '{id}' was not found.");

        // Rule 6: Al aprobar, se vuelve a validar la disponibilidad: pudo cambiar desde que se hizo la solicitud.
        var bookedUnits = await _context.Rentals
            .AsNoTracking()
            .Where(r => r.EquipmentId == rental.EquipmentId &&
                        r.Id != rental.Id &&
                        (r.Status == RentalStatus.Approved || r.Status == RentalStatus.Delivered) &&
                        r.StartDate <= rental.EndDate &&
                        r.EndDate >= rental.StartDate)
            .SumAsync(r => r.Quantity);

        var availableUnits = rental.Equipment.TotalUnits - bookedUnits;
        if (availableUnits < rental.Quantity)
        {
            throw new InvalidOperationException($"Cannot approve rental: insufficient equipment units available. Available: {availableUnits}, required: {rental.Quantity}.");
        }

        rental.Approve();
        await _context.SaveChangesAsync();

        return new RentalDto(
            rental.Id,
            rental.EquipmentId,
            rental.Equipment.Name,
            rental.ClientId,
            rental.Quantity,
            rental.StartDate,
            rental.EndDate,
            rental.TotalCost,
            rental.Status,
            rental.OverduePenalty,
            rental.Equipment.DailyRate
        );
    }

    public async Task<RentalDto> RejectRentalAsync(Guid id)
    {
        var rental = await _context.Rentals
            .Include(r => r.Equipment)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException($"Rental with ID '{id}' was not found.");

        rental.Reject();
        await _context.SaveChangesAsync();

        return new RentalDto(
            rental.Id,
            rental.EquipmentId,
            rental.Equipment.Name,
            rental.ClientId,
            rental.Quantity,
            rental.StartDate,
            rental.EndDate,
            rental.TotalCost,
            rental.Status,
            rental.OverduePenalty,
            rental.Equipment.DailyRate
        );
    }

    public async Task<RentalDto> DeliverRentalAsync(Guid id)
    {
        var rental = await _context.Rentals
            .Include(r => r.Equipment)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException($"Rental with ID '{id}' was not found.");

        rental.Deliver();
        await _context.SaveChangesAsync();

        return new RentalDto(
            rental.Id,
            rental.EquipmentId,
            rental.Equipment.Name,
            rental.ClientId,
            rental.Quantity,
            rental.StartDate,
            rental.EndDate,
            rental.TotalCost,
            rental.Status,
            rental.OverduePenalty,
            rental.Equipment.DailyRate
        );
    }

    public async Task<RentalDto> ReturnRentalAsync(Guid id, DateTime? returnDate = null)
    {
        var rental = await _context.Rentals
            .Include(r => r.Equipment)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException($"Rental with ID '{id}' was not found.");

        var effectiveReturnDate = returnDate ?? DateTime.UtcNow;
        rental.Return(DateTime.SpecifyKind(effectiveReturnDate, DateTimeKind.Utc), rental.Equipment.DailyRate);
        
        await _context.SaveChangesAsync();

        return new RentalDto(
            rental.Id,
            rental.EquipmentId,
            rental.Equipment.Name,
            rental.ClientId,
            rental.Quantity,
            rental.StartDate,
            rental.EndDate,
            rental.TotalCost,
            rental.Status,
            rental.OverduePenalty,
            rental.Equipment.DailyRate
        );
    }
}
