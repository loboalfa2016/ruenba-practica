using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FrameRent.Application.DTOs.Equipment;
using FrameRent.Application.Interfaces;

namespace FrameRent.API.Controllers;

[ApiController]
[Route("api/v1/equipment")]
public class EquipmentController : ControllerBase
{
    private readonly IEquipmentService _equipmentService;

    public EquipmentController(IEquipmentService equipmentService)
    {
        _equipmentService = equipmentService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetCatalog()
    {
        var catalog = await _equipmentService.GetActiveCatalogAsync();
        return Ok(catalog);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var equipment = await _equipmentService.GetByIdAsync(id);
        return Ok(equipment);
    }

    [HttpGet("{id:guid}/availability")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvailability(
        [FromRoute] Guid id,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        var availability = await _equipmentService.GetAvailabilityAsync(id, from, to);
        return Ok(availability);
    }

    [HttpPost]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> Create([FromBody] CreateEquipmentRequest request)
    {
        var created = await _equipmentService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateEquipmentRequest request)
    {
        var updated = await _equipmentService.UpdateAsync(id, request);
        return Ok(updated);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> Deactivate([FromRoute] Guid id)
    {
        await _equipmentService.DeactivateAsync(id);
        return NoContent();
    }
}
