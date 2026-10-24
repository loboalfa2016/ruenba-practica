using FrameRent.Application.DTOs.Equipment;

namespace FrameRent.Application.Interfaces;

public interface IEquipmentService
{
    Task<IEnumerable<EquipmentDto>> GetActiveCatalogAsync();
    Task<IEnumerable<EquipmentDto>> GetAllAsync();
    Task<EquipmentDto> GetByIdAsync(Guid id);
    Task<EquipmentAvailabilityDto> GetAvailabilityAsync(Guid id, DateTime from, DateTime to);
    Task<EquipmentDto> CreateAsync(CreateEquipmentRequest request);
    Task<EquipmentDto> UpdateAsync(Guid id, UpdateEquipmentRequest request);
    Task DeactivateAsync(Guid id);
    Task ActivateAsync(Guid id);
}
