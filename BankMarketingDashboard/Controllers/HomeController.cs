using System.Diagnostics;
using BankMarketingDashboard.Models;
using Microsoft.AspNetCore.Mvc;

/* =========================================================================
 *
 * Flujo general del programa:
 *   1. Se inyecta un logger en el constructor para permitir registrar eventos.
 *   2. Las acciones públicas (Index, Privacy, Error) responden a rutas y
 *      devuelven vistas simples.
 *   3. La acción Error construye un `ErrorViewModel` usando el identificador
 *      de petición actual para ayudar al diagnóstico cuando ocurre un fallo.
 *      
 *   - Este controlador atiende contenido mayormente estático; por claridad
 *     se mantiene separado de controladores que contienen lógica de negocio.
 *     
 * ========================================================================= */

namespace BankMarketingDashboard.Controllers
{
    /* ---------------------------------------------------------------------
     *   Contener las acciones básicas de la aplicación relacionadas con
     *   páginas informativas y manejo simple de errores.
     * --------------------------------------------------------------------- */
    public class HomeController : Controller
    {
        // Logger inyectado para registrar información relevante en ejecución.
        private readonly ILogger<HomeController> _logger;

        /* -----------------------------------------------------------------
         *   Constructor que recibe dependencias por inyección (logger).
         * ----------------------------------------------------------------- */
        public HomeController(ILogger<HomeController> logger)
        {
            // Guardamos el logger para uso en otras acciones si fuera necesario.
            _logger = logger;
        }

        /* ---------------------------------------------------------------------
         *   Acción que devuelve la página de inicio de la app.
         * --------------------------------------------------------------------- */
        public IActionResult Index()
        {
            // Retornamos la vista por convención: Views/Home/Index.cshtml.
            return View();
        }

        /* ---------------------------------------------------------------------
         *   Acción que devuelve la vista que contiene la política de privacidad.
         * --------------------------------------------------------------------- */
        public IActionResult Privacy()
        {
            // Mantener separadas las páginas informativas facilita el mantenimiento.
            return View();
        }

        /* ---------------------------------------------------------------------
         *   Acción que muestra una vista de error con información mínima para
         *   depuración (RequestId). Está decorada para evitar el almacenamiento
         *   en caché de respuestas de error.
         * --------------------------------------------------------------------- */
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Obtenemos un identificador que nos ayuda a rastrear la petición en logs.
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            // Construimos el modelo de error con el RequestId para mostrar en la vista.
            return View(new ErrorViewModel { RequestId = requestId });
        }
    }
}
