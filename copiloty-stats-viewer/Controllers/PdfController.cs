using Microsoft.AspNetCore.Mvc;
using copiloty_stats_viewer.Services;

namespace copiloty_stats_viewer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PdfController : ControllerBase
{
    private readonly PdfReportService _pdfReportService;

    public PdfController(PdfReportService pdfReportService)
    {
        _pdfReportService = pdfReportService;
    }

    [HttpGet("download")]
    public IActionResult DownloadReport([FromQuery] int costPerHour = 90, [FromQuery] int totalLicensedUsers = 0)
    {
        try
        {
            var pdfBytes = _pdfReportService.GenerateReport(costPerHour, totalLicensedUsers);
            var fileName = $"Copilot-Stats-Report-{DateTime.Now:yyyy-MM-dd-HHmm}.pdf";
            
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error generating PDF: {ex.Message}");
        }
    }
}