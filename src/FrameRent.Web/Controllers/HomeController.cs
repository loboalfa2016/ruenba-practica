using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FrameRent.Application.Interfaces;
using FrameRent.Web.Models;

namespace FrameRent.Web.Controllers;

public class HomeController : Controller
{
    private readonly IEquipmentService _equipmentService;

    public HomeController(IEquipmentService equipmentService)
    {
        _equipmentService = equipmentService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index(string? category, string? search)
    {
        var catalog = await _equipmentService.GetActiveCatalogAsync();

        var categories = catalog
            .Select(e => e.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        if (!string.IsNullOrWhiteSpace(category))
        {
            catalog = catalog.Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            catalog = catalog.Where(e => 
                e.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.Description.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var viewModel = new CatalogViewModel
        {
            Equipments = catalog,
            Categories = categories,
            SelectedCategory = category,
            SearchTerm = search
        };

        return View(viewModel);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Details(Guid id)
    {
        try
        {
            var equipment = await _equipmentService.GetByIdAsync(id);
            return View(equipment);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> CheckAvailability(Guid id, DateTime from, DateTime to)
    {
        try
        {
            var availability = await _equipmentService.GetAvailabilityAsync(id, from, to);
            return Json(new { success = true, data = availability });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
    }
}
