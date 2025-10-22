using Microsoft.AspNetCore.Mvc;
using BankMarketingDashboard.Services;
using BankMarketingDashboard.Models;
using Microsoft.Extensions.Logging;

namespace BankMarketingDashboard.Controllers
{
    public class DataUploadController : Controller
    {
        private readonly DataValidationService _validator;
        private readonly ILogger<DataUploadController> _logger;

        public DataUploadController(DataValidationService validator, ILogger<DataUploadController> logger)
        {
            _validator = validator;
            _logger = logger;
        }

        // GET: /DataUpload
        public IActionResult Index()
        {
            return View();
        }

        // POST: /DataUpload/Upload
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file provided." });
            }

            // Quick test shortcut: send ?test=1 to verify upload mechanics without parsing
            if (Request.Query.ContainsKey("test"))
            {
                return Json(new { ok = true, fileName = file.FileName, size = file.Length });
            }

            try
            {
                using var stream = file.OpenReadStream();
                var report = await _validator.ValidateCsvAsync(stream, Path.GetFileName(file.FileName));
                return Json(report);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Upload was cancelled.");
                return StatusCode(499, new { error = "Upload cancelled." });
            }
            catch (Exception ex)
            {
                // log full exception so you can find what crashed the server
                _logger.LogError(ex, "Error validating uploaded file '{FileName}'", file.FileName);
                return StatusCode(500, new { error = "Validation failed: " + ex.Message });
            }
        }
    }
}