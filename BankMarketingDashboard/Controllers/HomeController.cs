using System.Diagnostics;
using BankMarketingDashboard.Models;
using Microsoft.AspNetCore.Mvc;

/* =========================================================================
 *
 * Flujo general del programa:
 *   1. Se inyecta un logger en el constructor para permitir registrar eventos.
 *   2. Las acciones p�blicas (Index, Privacy, Error) responden a rutas y
 *      devuelven vistas simples.
 *   3. La acci�n Error construye un `ErrorViewModel` usando el identificador
 *      de petici�n actual para ayudar al diagn�stico cuando ocurre un fallo.
 *      
 *   - Este controlador atiende contenido mayormente est�tico; por claridad
 *     se mantiene separado de controladores que contienen l�gica de negocio.
 *     
 * ========================================================================= */

namespace BankMarketingDashboard.Controllers
{
    /* ---------------------------------------------------------------------
     *   Contener las acciones b�sicas de la aplicaci�n relacionadas con
     *   p�ginas informativas y manejo simple de errores.
     * --------------------------------------------------------------------- */
    public class HomeController : Controller
    {
        // Logger inyectado para registrar informaci�n relevante en ejecuci�n.
        private readonly ILogger<HomeController> _logger;

        /* -----------------------------------------------------------------
         *   Constructor que recibe dependencias por inyecci�n (logger).
         * ----------------------------------------------------------------- */
        public HomeController(ILogger<HomeController> logger)
        {
            // Guardamos el logger para uso en otras acciones si fuera necesario.
            _logger = logger;
        }

        /* ---------------------------------------------------------------------
         *   Acci�n que devuelve la p�gina de inicio de la app.
         * --------------------------------------------------------------------- */
        public IActionResult Index()
        {
            // Retornamos la vista por convenci�n: Views/Home/Index.cshtml.
            return View();
        }

        /* ---------------------------------------------------------------------
         *   Acci�n que devuelve la vista que contiene la pol�tica de privacidad.
         * --------------------------------------------------------------------- */
        public IActionResult Privacy()
        {
            // Mantener separadas las p�ginas informativas facilita el mantenimiento.
            return View();
        }

        /* ---------------------------------------------------------------------
         *   Acci�n que muestra una vista de error con informaci�n m�nima para
         *   depuraci�n (RequestId). Est� decorada para evitar el almacenamiento
         *   en cach� de respuestas de error.
         * --------------------------------------------------------------------- */
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Obtenemos un identificador que nos ayuda a rastrear la petici�n en logs.
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            // Construimos el modelo de error con el RequestId para mostrar en la vista.
            return View(new ErrorViewModel { RequestId = requestId });
        }
    }
}
