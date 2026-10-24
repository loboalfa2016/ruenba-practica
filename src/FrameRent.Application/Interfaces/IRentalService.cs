using FrameRent.Application.DTOs.Rental;
using FrameRent.Domain.Enums;

namespace FrameRent.Application.Interfaces;

public interface IRentalService
{
    Task<RentalDto> CreateRentalAsync(string clientId, CreateRentalRequest request);
    Task<IEnumerable<RentalDto>> GetCustomerRentalsAsync(string clientId);
    Task<RentalDto> GetRentalByIdAsync(Guid id, string userId, bool isStaff);
    Task CancelRentalAsync(Guid id, string userId, bool isStaff);
    Task<IEnumerable<RentalDto>> GetAllRentalsAsync(RentalStatus? status = null);
    Task<RentalDto> ApproveRentalAsync(Guid id);
    Task<RentalDto> RejectRentalAsync(Guid id);
    Task<RentalDto> DeliverRentalAsync(Guid id);
    Task<RentalDto> ReturnRentalAsync(Guid id, DateTime? returnDate = null);
}
