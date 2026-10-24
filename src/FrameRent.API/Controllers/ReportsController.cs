using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FrameRent.Application.Interfaces;

namespace FrameRent.API.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize(Roles = "Staff")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("topequipment")]
    public async Task<IActionResult> GetTopEquipment()
    {
        var report = await _reportService.GetTopEquipmentReportAsync();
        return Ok(report);
    }
}
