using Microsoft.AspNetCore.Mvc;
using BankMarketingDashboard.Models;
using BankMarketingDashboard.Services;

namespace BankMarketingDashboard.Controllers
{
    public class PredictionController : Controller
    {
        private readonly MLPredictionService _predictionService;
        private readonly ILogger<PredictionController> _logger;

        public PredictionController(
            MLPredictionService predictionService,
            ILogger<PredictionController> logger)
        {
            _predictionService = predictionService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new PredictionInput());
        }

        [HttpPost]
        public IActionResult Predict(PredictionInput input)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", input);
            }

            try
            {
                var result = _predictionService.Predict(input);
                ViewBag.Result = result;
                return View("Index", input);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar predicción");
                ModelState.AddModelError("", "Ocurrió un error al procesar la predicción.");
                return View("Index", input);
            }
        }

        [HttpPost]
        public async Task<IActionResult> RetrainModel()
        {
            try
            {
                await _predictionService.TrainModelAsync();
                TempData["Message"] = "Modelo reentrenado exitosamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reentrenar modelo");
                TempData["Error"] = "Error al reentrenar el modelo.";
            }

            return RedirectToAction("Index");
        }
    }
}

