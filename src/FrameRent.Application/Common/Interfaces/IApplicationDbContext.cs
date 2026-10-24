using Microsoft.EntityFrameworkCore;
using FrameRent.Domain.Entities;

namespace FrameRent.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Equipment> Equipments { get; }
    DbSet<Rental> Rentals { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
