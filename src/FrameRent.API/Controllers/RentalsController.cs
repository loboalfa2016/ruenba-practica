using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FrameRent.Application.DTOs.Rental;
using FrameRent.Application.Interfaces;
using FrameRent.Domain.Enums;

namespace FrameRent.API.Controllers;

[ApiController]
[Route("api/v1/rentals")]
[Authorize]
public class RentalsController : ControllerBase
{
    private readonly IRentalService _rentalService;

    public RentalsController(IRentalService rentalService)
    {
        _rentalService = rentalService;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) 
        ?? User.FindFirstValue(ClaimTypes.Name) 
        ?? string.Empty;

    private bool IsStaff => User.IsInRole("Staff");

    [HttpPost]
    [Authorize(Roles = "Client,Staff")]
    public async Task<IActionResult> CreateRental([FromBody] CreateRentalRequest request)
    {
        var created = await _rentalService.CreateRentalAsync(CurrentUserId, request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Client,Staff")]
    public async Task<IActionResult> GetMine()
    {
        var rentals = await _rentalService.GetCustomerRentalsAsync(CurrentUserId);
        return Ok(rentals);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var rental = await _rentalService.GetRentalByIdAsync(id, CurrentUserId, IsStaff);
        return Ok(rental);
    }

    [HttpPatch("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel([FromRoute] Guid id)
    {
        await _rentalService.CancelRentalAsync(id, CurrentUserId, IsStaff);
        return NoContent();
    }

    [HttpGet]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> GetAll([FromQuery] RentalStatus? status)
    {
        var rentals = await _rentalService.GetAllRentalsAsync(status);
        return Ok(rentals);
    }

    [HttpPatch("{id:guid}/approve")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> Approve([FromRoute] Guid id)
    {
        var approved = await _rentalService.ApproveRentalAsync(id);
        return Ok(approved);
    }

    [HttpPatch("{id:guid}/reject")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> Reject([FromRoute] Guid id)
    {
        var rejected = await _rentalService.RejectRentalAsync(id);
        return Ok(rejected);
    }

    [HttpPatch("{id:guid}/deliver")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> Deliver([FromRoute] Guid id)
    {
        var delivered = await _rentalService.DeliverRentalAsync(id);
        return Ok(delivered);
    }

    [HttpPatch("{id:guid}/return")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> Return([FromRoute] Guid id, [FromQuery] DateTime? returnDate)
    {
        var returned = await _rentalService.ReturnRentalAsync(id, returnDate);
        return Ok(returned);
    }
}
