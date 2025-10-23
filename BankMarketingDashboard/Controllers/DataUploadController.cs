using Microsoft.AspNetCore.Mvc;
using BankMarketingDashboard.Services;
using BankMarketingDashboard.Models;
using Microsoft.Extensions.Logging;
using System.IO;

/* =========================================================================
 *
 * Flujo general del programa:
 *   1. La vista de subida (GET) presenta un formulario para seleccionar el fichero.
 *   2. Al enviar el formulario, la acci�n `Upload` recibe el archivo.
 *   3. Si se activa `?test=1` se devuelve un peque�o JSON de confirmaci�n.
 *   4. En caso contrario se abre el stream del archivo y se llama a
 *      `_validator.ValidateCsvAsync(...)` para obtener el `DataQualityReport`.
 *   5. El resultado se devuelve como JSON; si ocurre un error se registra y
 *      se devuelve un c�digo HTTP apropiado con mensaje.
 *      
 * ========================================================================= */

namespace BankMarketingDashboard.Controllers
{
    /* ---------------------------------------------------------------------
     *   Controlador MVC que expone acciones para subir archivos CSV y obtener
     *   un informe de calidad sobre su contenido.
     * --------------------------------------------------------------------- */
    public class DataUploadController : Controller
    {
        // Servicio que realiza la validaci�n y parsing del CSV.
        private readonly DataValidationService _validator;

        // Logger para registrar informaci�n, advertencias y errores.
        private readonly ILogger<DataUploadController> _logger;

        /* -----------------------------------------------------------------
         *   Inicializa el controlador con las dependencias necesarias.
         * ----------------------------------------------------------------- */
        public DataUploadController(DataValidationService validator, ILogger<DataUploadController> logger)
        {
            _validator = validator;
            _logger = logger;
        }

        /* ---------------------------------------------------------------------
         *   Acci�n GET que devuelve la vista para subir archivos.
         * --------------------------------------------------------------------- */
        // GET: /DataUpload
        public IActionResult Index()
        {
            // No hay l�gica de negocio aqu�; la vista maneja la UI del formulario.
            return View();
        }

        /* ---------------------------------------------------------------------
         *   Acci�n POST que recibe un archivo (IFormFile) y solicita su validaci�n.
         *   Responde con JSON que contiene el `DataQualityReport` o un objeto de
         *   error cuando procede.
         * --------------------------------------------------------------------- */
        // POST: /DataUpload/Upload
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            // Validaci�n b�sica: comprobar que el cliente envi� un archivo.
            if (file == null || file.Length == 0)
            {
                // 400 Bad Request: el cliente no proporcion� el archivo esperado.
                return BadRequest(new { error = "No file provided." });
            }

            // Atajo �til para pruebas: si la petici�n incluye ?test se verifica que
            // la mec�nica de subida funciona sin realizar parsing del contenido.
            if (Request.Query.ContainsKey("test"))
            {
                // Devolver informaci�n m�nima: nombre y tama�o. Ayuda a depurar.
                return Json(new { ok = true, fileName = file.FileName, size = file.Length });
            }

            try
            {
                // Abrimos el stream del archivo y delegamos la validaci�n al servicio.
                // Usamos `using var` para garantizar el cierre del stream incluso en excepciones.
                using var stream = file.OpenReadStream();

                // Llamada as�ncrona al servicio que parsea y valida el CSV.
                // Se pasa el nombre de archivo para incluirlo en el reporte si es necesario.
                var report = await _validator.ValidateCsvAsync(stream, Path.GetFileName(file.FileName));

                // Devolvemos el informe como JSON para que el cliente lo consuma.
                return Json(report);
            }
            catch (OperationCanceledException)
            {
                // Caso en que la operaci�n fue cancelada por el cliente.
                _logger.LogWarning("Upload was cancelled.");
                // 499 Client Closed Request
                return StatusCode(499, new { error = "Upload cancelled." });
            }
            catch (Exception ex)
            {
                // Registrar el error completo para poder investigarlo en logs.
                _logger.LogError(ex, "Error validating uploaded file '{FileName}'", file.FileName);

                // Devolver 500 con mensaje resumido. No exponemos detalles internos.
                return StatusCode(500, new { error = "Validation failed: " + ex.Message });
            }
        }
    }
}