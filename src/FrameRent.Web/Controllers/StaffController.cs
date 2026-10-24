using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FrameRent.Application.Interfaces;
using FrameRent.Domain.Enums;
using FrameRent.Web.Models;

namespace FrameRent.Web.Controllers;

[Authorize(Roles = "Staff")]
public class StaffController : Controller
{
    private readonly IRentalService _rentalService;
    private readonly IReportService _reportService;

    public StaffController(IRentalService rentalService, IReportService reportService)
    {
        _rentalService = rentalService;
        _reportService = reportService;
    }

    [HttpGet]
    public async Task<IActionResult> Rentals(RentalStatus? status)
    {
        var rentals = await _rentalService.GetAllRentalsAsync(status);
        var viewModel = new StaffRentalsViewModel
        {
            Rentals = rentals,
            SelectedStatus = status
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id)
    {
        try
        {
            await _rentalService.ApproveRentalAsync(id);
            TempData["SuccessMessage"] = "Rental approved successfully!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Rentals));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id)
    {
        try
        {
            await _rentalService.RejectRentalAsync(id);
            TempData["SuccessMessage"] = "Rental was rejected.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Rentals));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deliver(Guid id)
    {
        try
        {
            await _rentalService.DeliverRentalAsync(id);
            TempData["SuccessMessage"] = "Equipment delivery recorded successfully!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Rentals));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(Guid id, DateTime? returnDate)
    {
        try
        {
            var returned = await _rentalService.ReturnRentalAsync(id, returnDate);
            if (returned.OverduePenalty > 0)
            {
                TempData["WarningMessage"] = $"Equipment returned late! Overdue penalty applied: ${returned.OverduePenalty:F2}.";
            }
            else
            {
                TempData["SuccessMessage"] = "Equipment returned on time with zero penalties.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Rentals));
    }

    [HttpGet]
    public async Task<IActionResult> TopEquipment()
    {
        var report = await _reportService.GetTopEquipmentReportAsync();
        return View(report);
    }
}
