using BankMarketingDashboard.Data;
using BankMarketingDashboard.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using ClosedXML.Excel;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

/* =========================================================================
 *   Controlador que expone una tabla interactiva con paginación y filtros
 *   aplicables desde la query string. También permite exportar los datos
 *   filtrados a CSV o Excel. La mayor parte del trabajo consiste en construir
 *   una consulta `IQueryable<CampaignRecord>` que representa los filtros
 *   solicitados, de modo que la base de datos haga el trabajo pesado.
 * ========================================================================= */

namespace BankMarketingDashboard.Controllers
{
    /* ---------------------------------------------------------------------
     *   Controlador responsable de presentar una tabla navegable y exportable
     *   de `CampaignRecord` con múltiples filtros y paginación.
     * --------------------------------------------------------------------- */
    public class InteractiveTableController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const int PageSize = 100; // tamaño de página fijo para la paginación

        /* -----------------------------------------------------------------
         *   Inyecta el contexto de datos que permite construir consultas EF.
         * ----------------------------------------------------------------- */
        public InteractiveTableController(ApplicationDbContext context)
        {
            _context = context;
        }

        /* ---------------------------------------------------------------------
         *   Acción principal que muestra la vista completa con la tabla paginada.
         *   Lee los filtros desde la query string, construye la consulta y
         *   ofrece información de paginación a la vista.
         * --------------------------------------------------------------------- */
        public IActionResult Index(
            int page = 1,
            [FromQuery(Name = "age_min")] int? minAge = null,
            [FromQuery(Name = "age_max")] int? maxAge = null,
            string[]? job = null,
            string[]? marital = null,
            string[]? education = null,
            string[]? month = null,
            string[]? y = null,
            string[]? subscribed = null,
            [FromQuery(Name = "default")] string[]? defaultFilter = null,
            string[]? housing = null,
            string[]? loan = null,
            string[]? contact = null,
            string[]? dayOfWeek = null,
            string[]? poutcome = null,
            [FromQuery(Name = "duration_min")] int? minDuration = null,
            [FromQuery(Name = "duration_max")] int? maxDuration = null,
            [FromQuery(Name = "campaign_min")] int? minCampaign = null,
            [FromQuery(Name = "campaign_max")] int? maxCampaign = null,
            [FromQuery(Name = "pdays_min")] int? minPdays = null,
            [FromQuery(Name = "pdays_max")] int? maxPdays = null,
            [FromQuery(Name = "previous_min")] int? minPrevious = null,
            [FromQuery(Name = "previous_max")] int? maxPrevious = null,
            [FromQuery(Name = "empvarrate_min")] double? minEmpVarRate = null,
            [FromQuery(Name = "empvarrate_max")] double? maxEmpVarRate = null,
            [FromQuery(Name = "conspriceidx_min")] double? minConsPriceIdx = null,
            [FromQuery(Name = "conspriceidx_max")] double? maxConsPriceIdx = null,
            [FromQuery(Name = "consconfidx_min")] double? minConsConfIdx = null,
            [FromQuery(Name = "consconfidx_max")] double? maxConsConfIdx = null,
            [FromQuery(Name = "euribor3m_min")] double? minEuribor3m = null,
            [FromQuery(Name = "euribor3m_max")] double? maxEuribor3m = null,
            [FromQuery(Name = "nremployed_min")] double? minNrEmployed = null,
            [FromQuery(Name = "nremployed_max")] double? maxNrEmployed = null)
        {
            // Normalizar la página para evitar valores inválidos
            if (page < 1) page = 1;

            // Construimos la consulta filtrada (sin materializar aún)
            var query = BuildFilteredQuery(
                minAge, maxAge,
                job, marital, education, month, y, subscribed,
                defaultFilter, housing, loan, contact, dayOfWeek, poutcome,
                minDuration, maxDuration, minCampaign, maxCampaign,
                minPdays, maxPdays, minPrevious, maxPrevious,
                minEmpVarRate, maxEmpVarRate, minConsPriceIdx, maxConsPriceIdx,
                minConsConfIdx, maxConsConfIdx, minEuribor3m, maxEuribor3m,
                minNrEmployed, maxNrEmployed);

            // Contamos y paginamos; Count se traduce a SQL COUNT por EF Core.
            var totalRecords = query.Count();
            var totalPages = (int)System.Math.Ceiling((double)totalRecords / PageSize);

            // Materializamos solo la página solicitada
            var records = query
                .OrderBy(c => c.Age) // orden razonable y estable para paginación
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // Pasamos datos de paginación/filtros a la vista mediante ViewBag
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalRecords = totalRecords;

            ViewBag.FilterMinAge = minAge;
            ViewBag.FilterMaxAge = maxAge;
            ViewBag.FilterJob = job != null ? string.Join(",", job) : "";
            ViewBag.FilterMarital = marital != null ? string.Join(",", marital) : "";
            ViewBag.FilterEducation = education != null ? string.Join(",", education) : "";
            ViewBag.FilterMonth = month != null ? string.Join(",", month) : "";
            ViewBag.FilterY = y != null ? string.Join(",", y) : "";
            ViewBag.FilterSubscribed = subscribed != null ? string.Join(",", subscribed) : ""; 

            ViewBag.FilterDefault = defaultFilter != null ? string.Join(",", defaultFilter) : "";
            ViewBag.FilterHousing = housing != null ? string.Join(",", housing) : "";
            ViewBag.FilterLoan = loan != null ? string.Join(",", loan) : "";
            ViewBag.FilterContact = contact != null ? string.Join(",", contact) : "";
            ViewBag.FilterDayOfWeek = dayOfWeek != null ? string.Join(",", dayOfWeek) : "";
            ViewBag.FilterPoutcome = poutcome != null ? string.Join(",", poutcome) : "";

            // Rango: duración de la llamada
            ViewBag.MinDuration = minDuration;
            ViewBag.MaxDuration = maxDuration;
            // Rango: número de campañas
            ViewBag.MinCampaign = minCampaign;
            ViewBag.MaxCampaign = maxCampaign;
            // Rango: días desde último contacto
            ViewBag.MinPdays = minPdays;
            ViewBag.MaxPdays = maxPdays;
            // Rango: contactos previos
            ViewBag.MinPrevious = minPrevious;
            ViewBag.MaxPrevious = maxPrevious;

            // Rango: tasas/índices económicos
            ViewBag.MinEmpVarRate = minEmpVarRate;
            ViewBag.MaxEmpVarRate = maxEmpVarRate;
            ViewBag.MinConsPriceIdx = minConsPriceIdx;
            ViewBag.MaxConsPriceIdx = maxConsPriceIdx;
            ViewBag.MinConsConfIdx = minConsConfIdx;
            ViewBag.MaxConsConfIdx = maxConsConfIdx;
            ViewBag.MinEuribor3m = minEuribor3m;
            ViewBag.MaxEuribor3m = maxEuribor3m;
            ViewBag.MinNrEmployed = minNrEmployed;
            ViewBag.MaxNrEmployed = maxNrEmployed;

            // Retornamos la vista principal con la página de registros
            return View(records);
        }

        /* ---------------------------------------------------------------------
         *   Acción que devuelve solo la porción de la tabla (partial view).
         *   Se usa para actualizar la tabla mediante peticiones AJAX sin recargar
         *   la página completa.
         * --------------------------------------------------------------------- */
        [HttpGet]
        // Acción parcial para cargar solo la tabla
        public IActionResult TablePartial(
            int page = 1,
            [FromQuery(Name = "age_min")] int? minAge = null,
            [FromQuery(Name = "age_max")] int? maxAge = null,
            string[]? job = null,
            string[]? marital = null,
            string[]? education = null,
            string[]? month = null,
            string[]? y = null,
            string[]? subscribed = null,
            [FromQuery(Name = "default")] string[]? defaultFilter = null,
            string[]? housing = null,
            string[]? loan = null,
            string[]? contact = null,
            string[]? dayOfWeek = null,
            string[]? poutcome = null,
            [FromQuery(Name = "duration_min")] int? minDuration = null,
            [FromQuery(Name = "duration_max")] int? maxDuration = null,
            [FromQuery(Name = "campaign_min")] int? minCampaign = null,
            [FromQuery(Name = "campaign_max")] int? maxCampaign = null,
            int? minPdays = null,
            int? maxPdays = null,
            [FromQuery(Name = "previous_min")] int? minPrevious = null,
            [FromQuery(Name = "previous_max")] int? maxPrevious = null,
            double? minEmpVarRate = null,
            double? maxEmpVarRate = null,
            double? minConsPriceIdx = null,
            double? maxConsPriceIdx = null,
            double? minConsConfIdx = null,
            double? maxConsConfIdx = null,
            [FromQuery(Name = "euribor3m_min")] double? minEuribor3m = null,
            [FromQuery(Name = "euribor3m_max")] double? maxEuribor3m = null,
            [FromQuery(Name = "nremployed_min")] double? minNrEmployed = null,
            [FromQuery(Name = "nremployed_max")] double? maxNrEmployed = null)
        {
            // Línea de depuración para verificar parámetros en tiempo de desarrollo
            System.Diagnostics.Debug.WriteLine($"TablePartial called: page={page}, minPdays={minPdays}, maxPdays={maxPdays}, minCampaign={minCampaign}, maxCampaign={maxCampaign}, minPrevious={minPrevious}, maxPrevious={maxPrevious}, minEmpVarRate={minEmpVarRate}, maxEmpVarRate={maxEmpVarRate}, minConsPriceIdx={minConsPriceIdx}, maxConsPriceIdx={maxConsPriceIdx}, minConsConfIdx={minConsConfIdx}, maxConsConfIdx={maxConsConfIdx}");
            if (page < 1) page = 1;

            // Construimos la misma consulta que en `Index`
            var query = BuildFilteredQuery(
                minAge, maxAge,
                job, marital, education, month, y, subscribed,     // pasar subscribe' al helper de filtros
                defaultFilter, housing, loan, contact, dayOfWeek, poutcome,
                minDuration, maxDuration, minCampaign, maxCampaign,
                minPdays, maxPdays, minPrevious, maxPrevious,
                minEmpVarRate, maxEmpVarRate, minConsPriceIdx, maxConsPriceIdx,
                minConsConfIdx, maxConsConfIdx, minEuribor3m, maxEuribor3m,
                minNrEmployed, maxNrEmployed);

            var totalRecords = query.Count();
            var totalPages = (int)System.Math.Ceiling((double)totalRecords / PageSize);

            var records = query
                .OrderBy(c => c.Age)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // Pasamos datos a la partial igual que en `Index` para consistencia
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalRecords = totalRecords;

            ViewBag.FilterMinAge = minAge;
            ViewBag.FilterMaxAge = maxAge;
            ViewBag.FilterJob = job != null ? string.Join(",", job) : "";
            ViewBag.FilterMarital = marital != null ? string.Join(",", marital) : "";
            ViewBag.FilterEducation = education != null ? string.Join(",", education) : "";
            ViewBag.FilterMonth = month != null ? string.Join(",", month) : "";
            ViewBag.FilterY = y != null ? string.Join(",", y) : "";
            ViewBag.FilterSubscribed = subscribed != null ? string.Join(",", subscribed) : ""; // estado del filtro "suscrito"

            ViewBag.FilterDefault = defaultFilter != null ? string.Join(",", defaultFilter) : "";
            ViewBag.FilterHousing = housing != null ? string.Join(",", housing) : "";
            ViewBag.FilterLoan = loan != null ? string.Join(",", loan) : "";
            ViewBag.FilterContact = contact != null ? string.Join(",", contact) : "";
            ViewBag.FilterDayOfWeek = dayOfWeek != null ? string.Join(",", dayOfWeek) : "";
            ViewBag.FilterPoutcome = poutcome != null ? string.Join(",", poutcome) : "";

            ViewBag.MinDuration = minDuration;
            ViewBag.MaxDuration = maxDuration;
            ViewBag.MinCampaign = minCampaign;
            ViewBag.MaxCampaign = maxCampaign;
            ViewBag.MinPdays = minPdays;
            ViewBag.MaxPdays = maxPdays;
            ViewBag.MinPrevious = minPrevious;
            ViewBag.MaxPrevious = maxPrevious;

            ViewBag.MinEmpVarRate = minEmpVarRate;
            ViewBag.MaxEmpVarRate = maxEmpVarRate;
            ViewBag.MinConsPriceIdx = minConsPriceIdx;
            ViewBag.MaxConsPriceIdx = maxConsPriceIdx;
            ViewBag.MinConsConfIdx = minConsConfIdx;
            ViewBag.MaxConsConfIdx = maxConsConfIdx;
            ViewBag.MinEuribor3m = minEuribor3m;
            ViewBag.MaxEuribor3m = maxEuribor3m;
            ViewBag.MinNrEmployed = minNrEmployed;
            ViewBag.MaxNrEmployed = maxNrEmployed;

            return PartialView("_InteractiveTablePartial", records);
        }

        // Exportar CSV aplicando los mismos filtros que la vista Index/TablePartial
        public IActionResult ExportCsv(
            [FromQuery(Name = "age_min")] int? minAge = null,
            [FromQuery(Name = "age_max")] int? maxAge = null,
            string[]? job = null,
            string[]? marital = null,
            string[]? education = null,
            string[]? month = null,
            string[]? y = null,
            string[]? subscribed = null,
            [FromQuery(Name = "default")] string[]? defaultFilter = null,
            string[]? housing = null,
            string[]? loan = null,
            string[]? contact = null,
            string[]? dayOfWeek = null,
            string[]? poutcome = null,
            [FromQuery(Name = "duration_min")] int? minDuration = null,
            [FromQuery(Name = "duration_max")] int? maxDuration = null,
            [FromQuery(Name = "campaign_min")] int? minCampaign = null,
            [FromQuery(Name = "campaign_max")] int? maxCampaign = null,
            [FromQuery(Name = "pdays_min")] int? minPdays = null,
            [FromQuery(Name = "pdays_max")] int? maxPdays = null,
            [FromQuery(Name = "previous_min")] int? minPrevious = null,
            [FromQuery(Name = "previous_max")] int? maxPrevious = null,
            [FromQuery(Name = "empvarrate_min")] double? minEmpVarRate = null,
            [FromQuery(Name = "empvarrate_max")] double? maxEmpVarRate = null,
            [FromQuery(Name = "conspriceidx_min")] double? minConsPriceIdx = null,
            [FromQuery(Name = "conspriceidx_max")] double? maxConsPriceIdx = null,
            [FromQuery(Name = "consconfidx_min")] double? minConsConfIdx = null,
            [FromQuery(Name = "consconfidx_max")] double? maxConsConfIdx = null,
            [FromQuery(Name = "euribor3m_min")] double? minEuribor3m = null,
            [FromQuery(Name = "euribor3m_max")] double? maxEuribor3m = null,
            double? minNrEmpleados = null,
            double? maxNrEmpleados = null)
        {
            try
            {
                // Materializamos todos los registros filtrados para exportar
                var query = BuildFilteredQuery(
                    minAge, maxAge,
                    job, marital, education, month, y, subscribed,
                    defaultFilter, housing, loan, contact, dayOfWeek, poutcome,
                    minDuration, maxDuration, minCampaign, maxCampaign,
                    minPdays, maxPdays, minPrevious, maxPrevious,
                    minEmpVarRate, maxEmpVarRate, minConsPriceIdx, maxConsPriceIdx,
                    minConsConfIdx, maxConsConfIdx, minEuribor3m, maxEuribor3m,
                    minNrEmpleados, maxNrEmpleados);

                var records = query.ToList();

                // Construcción manual del CSV; escapamos comillas dobles según RFC
                var csv = new StringBuilder();
                csv.AppendLine("Age,Job,Marital,Education,Default,Housing,Loan,Contact,Month,DayOfWeek,Duration,Campaign,Pdays,Previous,Poutcome,EmpVarRate,ConsPriceIdx,ConsConfIdx,Euribor3m,NrEmployed,Y");

                string Escape(object? o) => o == null ? "" : $"\"{o.ToString().Replace("\"", "\"\"")}\"";

                foreach (var r in records)
                {
                    csv.AppendLine(string.Join(",",
                        Escape(r.Age),
                        Escape(r.Job),
                        Escape(r.Marital),
                        Escape(r.Education),
                        Escape(r.Default),
                        Escape(r.Housing),
                        Escape(r.Loan),
                        Escape(r.Contact),
                        Escape(r.Month),
                        Escape(r.DayOfWeek),
                        Escape(r.Duration),
                        Escape(r.Campaign),
                        Escape(r.Pdays),
                        Escape(r.Previous),
                        Escape(r.Poutcome),
                        Escape(r.EmpVarRate),
                        Escape(r.ConsPriceIdx),
                        Escape(r.ConsConfIdx),
                        Escape(r.Euribor3m),
                        Escape(r.NrEmployed),
                        Escape(r.Y)
                    ));
                }

                // Guardamos en archivo temporal y devolvemos como PhysicalFile.
                // Añadimos BOM para compatibilidad con Excel/Notepad en algunos sistemas.
                var tempPath = Path.Combine(Path.GetTempPath(), $"bankmarketing_{System.Guid.NewGuid()}.csv");
                System.IO.File.WriteAllText(tempPath, "\uFEFF" + csv.ToString(), new UTF8Encoding(true));

                // Programamos la eliminación del archivo temporal en segundo plano
                // para no dejar basura en disco si el cliente no lo descarga.
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromMinutes(5));
                    try { System.IO.File.Delete(tempPath); } catch { }
                });

                return PhysicalFile(tempPath, "text/csv; charset=utf-8", "bankmarketing_export_filtered.csv");
            }
            catch (System.Exception ex)
            {
                // Registramos el error en debug
                System.Diagnostics.Debug.WriteLine($"Export CSV error: {ex}");
                return StatusCode(500, "CSV export failed.");
            }
        }

        /* ---------------------------------------------------------------------
         *   Genera y devuelve un archivo Excel (.xlsx) con los registros filtrados.
         * --------------------------------------------------------------------- */
        public IActionResult ExportExcel(
            [FromQuery(Name = "age_min")] int? minAge = null,
            [FromQuery(Name = "age_max")] int? maxAge = null,
            string[]? job = null,
            string[]? marital = null,
            string[]? education = null,
            string[]? month = null,
            string[]? y = null,
            string[]? subscribed = null,
            [FromQuery(Name = "default")] string[]? defaultFilter = null,
            string[]? housing = null,
            string[]? loan = null,
            string[]? contact = null,
            string[]? dayOfWeek = null,
            string[]? poutcome = null,
            [FromQuery(Name = "duration_min")] int? minDuration = null,
            [FromQuery(Name = "duration_max")] int? maxDuration = null,
            [FromQuery(Name = "campaign_min")] int? minCampaign = null,
            [FromQuery(Name = "campaign_max")] int? maxCampaign = null,
            [FromQuery(Name = "pdays_min")] int? minPdays = null,
            [FromQuery(Name = "pdays_max")] int? maxPdays = null,
            [FromQuery(Name = "previous_min")] int? minPrevious = null,
            [FromQuery(Name = "previous_max")] int? maxPrevious = null,
            [FromQuery(Name = "empvarrate_min")] double? minEmpVarRate = null,
            [FromQuery(Name = "empvarrate_max")] double? maxEmpVarRate = null,
            [FromQuery(Name = "conspriceidx_min")] double? minConsPriceIdx = null,
            [FromQuery(Name = "conspriceidx_max")] double? maxConsPriceIdx = null,
            [FromQuery(Name = "consconfidx_min")] double? minConsConfIdx = null,
            [FromQuery(Name = "consconfidx_max")] double? maxConsConfIdx = null,
            [FromQuery(Name = "euribor3m_min")] double? minEuribor3m = null,
            [FromQuery(Name = "euribor3m_max")] double? maxEuribor3m = null,
            [FromQuery(Name = "nremployed_min")] double? minNrEmpleados = null,
            [FromQuery(Name = "nremployed_max")] double? maxNrEmpleados = null)
        {
            try
            {
                // Materializamos la consulta filtrada
                var query = BuildFilteredQuery(
                    minAge, maxAge,
                    job, marital, education, month, y, subscribed,
                    defaultFilter, housing, loan, contact, dayOfWeek, poutcome,
                    minDuration, maxDuration, minCampaign, maxCampaign,
                    minPdays, maxPdays, minPrevious, maxPrevious,
                    minEmpVarRate, maxEmpVarRate, minConsPriceIdx, maxConsPriceIdx,
                    minConsConfIdx, maxConsConfIdx, minEuribor3m, maxEuribor3m,
                    minNrEmpleados, maxNrEmpleados);

                var records = query.ToList();

                // Creamos un archivo Excel temporal usando ClosedXML
                var tempPath = Path.Combine(Path.GetTempPath(), $"bankmarketing_{System.Guid.NewGuid()}.xlsx");

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Filtered Data");

                    var headers = new[]
                    {
                        "Age", "Job", "Marital", "Education", "Default", "Housing", "Loan", "Contact",
                        "Month", "DayOfWeek", "Duration", "Campaign", "Pdays", "Previous", "Poutcome",
                        "EmpVarRate", "ConsPriceIdx", "ConsConfIdx", "Euribor3m", "NrEmpleados", "Y"
                    };

                    // Escribimos cabeceras con estilo sencillo para que el archivo sea legible
                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = headers[i];
                        worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                        worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#CB3CFF");
                        worksheet.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
                    }

                    // Rellenamos filas con los datos
                    for (int i = 0; i < records.Count; i++)
                    {
                        var r = records[i];
                        worksheet.Cell(i + 2, 1).Value = r.Age;
                        worksheet.Cell(i + 2, 2).Value = r.Job;
                        worksheet.Cell(i + 2, 3).Value = r.Marital;
                        worksheet.Cell(i + 2, 4).Value = r.Education;
                        worksheet.Cell(i + 2, 5).Value = r.Default;
                        worksheet.Cell(i + 2, 6).Value = r.Housing;
                        worksheet.Cell(i + 2, 7).Value = r.Loan;
                        worksheet.Cell(i + 2, 8).Value = r.Contact;
                        worksheet.Cell(i + 2, 9).Value = r.Month;
                        worksheet.Cell(i + 2, 10).Value = r.DayOfWeek;
                        worksheet.Cell(i + 2, 11).Value = r.Duration;
                        worksheet.Cell(i + 2, 12).Value = r.Campaign;
                        worksheet.Cell(i + 2, 13).Value = r.Pdays;
                        worksheet.Cell(i + 2, 14).Value = r.Previous;
                        worksheet.Cell(i + 2, 15).Value = r.Poutcome;
                        worksheet.Cell(i + 2, 16).Value = r.EmpVarRate;
                        worksheet.Cell(i + 2, 17).Value = r.ConsPriceIdx;
                        worksheet.Cell(i + 2, 18).Value = r.ConsConfIdx;
                        worksheet.Cell(i + 2, 19).Value = r.Euribor3m;
                        worksheet.Cell(i + 2, 20).Value = r.NrEmployed;
                        worksheet.Cell(i + 2, 21).Value = r.Y;
                    }

                    // Ajuste de ancho para mejor presentación
                    worksheet.Columns().AdjustToContents();
                    workbook.SaveAs(tempPath);
                }

                // Programamos la eliminación del archivo temporal como en ExportCsv
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromMinutes(5));
                    try { System.IO.File.Delete(tempPath); } catch { }
                });

                return PhysicalFile(tempPath, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "bankmarketing_export_filtered.xlsx");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Export Excel error: {ex}");
                return StatusCode(500, "Excel export failed.");
            }
        }

        /* ---------------------------------------------------------------------
         *   Helper que construye y devuelve un `IQueryable<CampaignRecord>` que
         *   aplica todos los filtros recibidos como parámetros. La consulta
         *   resultante está preparada para ser paginada o materializada por el caller.
         * --------------------------------------------------------------------- */
        private IQueryable<CampaignRecord> BuildFilteredQuery(
            int? minAge, int? maxAge,
            string[]? job, string[]? marital, string[]? education, string[]? month, string[]? y, string[]? subscribed, 
            string[]? defaultFilter, string[]? housing, string[]? loan, string[]? contact, string[]? dayOfWeek, string[]? poutcome,
            int? minDuration, int? maxDuration, int? minCampaign, int? maxCampaign,
            int? minPdays, int? maxPdays, int? minPrevious, int? maxPrevious,
            double? minEmpVarRate, double? maxEmpVarRate, double? minConsPriceIdx, double? maxConsPriceIdx,
            double? minConsConfIdx, double? maxConsConfIdx, double? minEuribor3m, double? maxEuribor3m,
            double? minNrEmpleados, double? maxNrEmpleados)
        {
            // Partimos de todos los registros y añadimos filtros sucesivamente
            var query = _context.CampaignData.AsQueryable();

            // Rangos de edad
            if (minAge.HasValue) query = query.Where(c => c.Age >= minAge.Value);
            if (maxAge.HasValue) query = query.Where(c => c.Age <= maxAge.Value);

            // Job: coincidencia parcial insensible a mayúsculas/minúsculas mediante EF.Functions.Like
            // Usamos expresiones dinámicas para construir una condición OR entre varios `LIKE '%val%'`
            if (job != null && job.Any(s => !string.IsNullOrWhiteSpace(s)))
            {
                var arr = job.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.ToLower()).ToArray();
                if (arr.Length > 0)
                {
                    var param = System.Linq.Expressions.Expression.Parameter(typeof(CampaignRecord), "c");
                    System.Linq.Expressions.Expression? body = null;

                    // c.Job ?? ""  --> evita nulls antes de hacer ToLower
                    var jobProp = System.Linq.Expressions.Expression.Property(param, nameof(CampaignRecord.Job));
                    var coalesce = System.Linq.Expressions.Expression.Coalesce(jobProp, System.Linq.Expressions.Expression.Constant(""));
                    var toLowerMethod = typeof(string).GetMethod("ToLower", System.Type.EmptyTypes)!;
                    var toLowerExpr = System.Linq.Expressions.Expression.Call(coalesce, toLowerMethod);

                    // EF.Functions para que EF Core traduzca Like a SQL LIKE
                    var efFunctionsProp = typeof(Microsoft.EntityFrameworkCore.EF).GetProperty(nameof(Microsoft.EntityFrameworkCore.EF.Functions))!;
                    var efFunctionsExpr = System.Linq.Expressions.Expression.Property(null, efFunctionsProp);

                    var likeMethod = typeof(Microsoft.EntityFrameworkCore.DbFunctionsExtensions)
                        .GetMethod(nameof(Microsoft.EntityFrameworkCore.DbFunctionsExtensions.Like), new[] { typeof(Microsoft.EntityFrameworkCore.DbFunctions), typeof(string), typeof(string) })!;

                    // Construimos OR(like(..., '%val1%'), like(..., '%val2%'), ...)
                    foreach (var val in arr)
                    {
                        var pattern = System.Linq.Expressions.Expression.Constant("%" + val + "%");
                        var likeCall = System.Linq.Expressions.Expression.Call(null, likeMethod, efFunctionsExpr, toLowerExpr, pattern);
                        body = body == null ? likeCall : System.Linq.Expressions.Expression.OrElse(body, likeCall);
                    }

                    var lambda = System.Linq.Expressions.Expression.Lambda<Func<CampaignRecord, bool>>(body!, param);
                    // Aplicamos la condición a la consulta; se traducirá a SQL y será eficiente
                    query = query.Where(lambda);
                }
            }

            // Marital: coincidencia exacta insensible a mayúsculas/minúsculas
            if (marital != null && marital.Any(s => !string.IsNullOrWhiteSpace(s)))
            {
                var arr = marital.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.ToLower()).ToArray();
                // Convertimos both sides a minúsculas para comparar sin case-sensitivity
                query = query.Where(c => arr.Contains((c.Marital ?? "").ToLower()));
            }

            // Education: coincidencia parcial tipo Job
            if (education != null && education.Any(s => !string.IsNullOrWhiteSpace(s)))
            {
                var arr = education.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.ToLower()).ToArray();
                if (arr.Length > 0)
                {
                    var param = System.Linq.Expressions.Expression.Parameter(typeof(CampaignRecord), "c");
                    System.Linq.Expressions.Expression? body = null;

                    var eduProp = System.Linq.Expressions.Expression.Property(param, nameof(CampaignRecord.Education));
                    var coalesce = System.Linq.Expressions.Expression.Coalesce(eduProp, System.Linq.Expressions.Expression.Constant(""));
                    var toLowerMethod = typeof(string).GetMethod("ToLower", System.Type.EmptyTypes)!;
                    var toLowerExpr = System.Linq.Expressions.Expression.Call(coalesce, toLowerMethod);

                    var efFunctionsProp = typeof(Microsoft.EntityFrameworkCore.EF).GetProperty(nameof(Microsoft.EntityFrameworkCore.EF.Functions))!;
                    var efFunctionsExpr = System.Linq.Expressions.Expression.Property(null, efFunctionsProp);

                    var likeMethod = typeof(Microsoft.EntityFrameworkCore.DbFunctionsExtensions)
                        .GetMethod(nameof(Microsoft.EntityFrameworkCore.DbFunctionsExtensions.Like), new[] { typeof(Microsoft.EntityFrameworkCore.DbFunctions), typeof(string), typeof(string) })!;

                    foreach (var val in arr)
                    {
                        var pattern = System.Linq.Expressions.Expression.Constant("%" + val + "%");
                        var likeCall = System.Linq.Expressions.Expression.Call(null, likeMethod, efFunctionsExpr, toLowerExpr, pattern);
                        body = body == null ? likeCall : System.Linq.Expressions.Expression.OrElse(body, likeCall);
                    }

                    var lambda = System.Linq.Expressions.Expression.Lambda<Func<CampaignRecord, bool>>(body!, param);
                    query = query.Where(lambda);
                }
            }

            // Month: coincidencia exacta insensible a mayúsculas/minúsculas
            if (month != null && month.Any(s => !string.IsNullOrWhiteSpace(s)))
            {
                var arr = month.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.ToLower()).ToArray();
                query = query.Where(c => arr.Contains((c.Month ?? "").ToLower()));
            }

            // Y/subscribed: admitir ambos parámetros, subscribed tiene prioridad
            // Esto permite compatibilidad con diferentes nombres de filtro desde la UI
            var subsInput = subscribed ?? y;
            if (subsInput != null && subsInput.Any(s => !string.IsNullOrWhiteSpace(s)))
            {
                var arr = subsInput.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.ToLower()).ToArray();
                query = query.Where(c => arr.Contains((c.Y ?? "").ToLower()));
            }

            // Filtros categóricos adicionales (comparación exacta, normalizando a minúsculas)
            if (defaultFilter != null && defaultFilter.Any(s => !string.IsNullOrWhiteSpace(s)))
            {
                var arr = defaultFilter.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.ToLower()).ToArray();
                query = query.Where(c => arr.Contains((c.Default ?? "").ToLower()));
            }

            if (housing != null && housing.Any(s => !string.IsNullOrWhiteSpace(s)))
            {
                var arr = housing.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.ToLower()).ToArray();
                query = query.Where(c => arr.Contains((c.Housing ?? "").ToLower()));
            }

            if (loan != null && loan.Any(s => !string.IsNullOrWhiteSpace(s)))
            {
                var arr = loan.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.ToLower()).ToArray();
                query = query.Where(c => arr.Contains((c.Loan ?? "").ToLower()));
            }

            if (contact != null && contact.Any(s => !string.IsNullOrWhiteSpace(s)))
            {
                var arr = contact.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.ToLower()).ToArray();
                query = query.Where(c => arr.Contains((c.Contact ?? "").ToLower()));
            }

            if (dayOfWeek != null && dayOfWeek.Any(s => !string.IsNullOrWhiteSpace(s)))
            {
                var arr = dayOfWeek.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.ToLower()).ToArray();
                query = query.Where(c => arr.Contains((c.DayOfWeek ?? "").ToLower()));
            }

            if (poutcome != null && poutcome.Any(s => !string.IsNullOrWhiteSpace(s)))
            {
                var arr = poutcome.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.ToLower()).ToArray();
                query = query.Where(c => arr.Contains((c.Poutcome ?? "").ToLower()));
            }

            // Rangos numéricos (enteros)
            if (minDuration.HasValue) query = query.Where(c => c.Duration >= minDuration.Value);
            if (maxDuration.HasValue) query = query.Where(c => c.Duration <= maxDuration.Value);

            if (minCampaign.HasValue) query = query.Where(c => c.Campaign >= minCampaign.Value);
            if (maxCampaign.HasValue) query = query.Where(c => c.Campaign <= maxCampaign.Value);

            if (minPdays.HasValue) query = query.Where(c => c.Pdays >= minPdays.Value);
            if (maxPdays.HasValue) query = query.Where(c => c.Pdays <= maxPdays.Value);

            if (minPrevious.HasValue) query = query.Where(c => c.Previous >= minPrevious.Value);
            if (maxPrevious.HasValue) query = query.Where(c => c.Previous <= maxPrevious.Value);

            // Rangos numéricos (double) para tasas e índices económicos
            if (minEmpVarRate.HasValue) query = query.Where(c => c.EmpVarRate >= minEmpVarRate.Value);
            if (maxEmpVarRate.HasValue) query = query.Where(c => c.EmpVarRate <= maxEmpVarRate.Value);

            if (minConsPriceIdx.HasValue) query = query.Where(c => c.ConsPriceIdx >= minConsPriceIdx.Value);
            if (maxConsPriceIdx.HasValue) query = query.Where(c => c.ConsPriceIdx <= maxConsPriceIdx.Value);

            if (minConsConfIdx.HasValue) query = query.Where(c => c.ConsConfIdx >= minConsConfIdx.Value);
            if (maxConsConfIdx.HasValue) query = query.Where(c => c.ConsConfIdx <= maxConsConfIdx.Value);

            if (minEuribor3m.HasValue) query = query.Where(c => c.Euribor3m >= minEuribor3m.Value);
            if (maxEuribor3m.HasValue) query = query.Where(c => c.Euribor3m <= maxEuribor3m.Value);

            if (minNrEmpleados.HasValue) query = query.Where(c => c.NrEmployed >= minNrEmpleados.Value);
            if (maxNrEmpleados.HasValue) query = query.Where(c => c.NrEmployed <= maxNrEmpleados.Value);

            // Devolvemos la consulta aún sin materializar
            return query;
        }
    }
}