using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FrameRent.Application.DTOs.Rental;
using FrameRent.Application.Interfaces;
using FrameRent.Web.Models;

namespace FrameRent.Web.Controllers;

[Authorize]
public class RentalsController : Controller
{
    private readonly IRentalService _rentalService;
    private readonly IEquipmentService _equipmentService;

    public RentalsController(IRentalService rentalService, IEquipmentService equipmentService)
    {
        _rentalService = rentalService;
        _equipmentService = equipmentService;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    [HttpGet]
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> Mine()
    {
        var rentals = await _rentalService.GetCustomerRentalsAsync(CurrentUserId);
        return View(rentals);
    }

    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookRentalViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please fill in all rental fields correctly.";
            return RedirectToAction("Index", "Home");
        }

        try
        {
            var request = new CreateRentalRequest(
                model.EquipmentId,
                model.Quantity,
                model.StartDate,
                model.EndDate
            );

            await _rentalService.CreateRentalAsync(CurrentUserId, request);
            TempData["SuccessMessage"] = "Rental request created successfully! It is now pending approval.";
            return RedirectToAction(nameof(Mine));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            await _rentalService.CancelRentalAsync(id, CurrentUserId, isStaff: false);
            TempData["SuccessMessage"] = "Rental was successfully canceled.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Mine));
    }
}
