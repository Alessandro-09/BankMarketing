using Microsoft.AspNetCore.Mvc;
using BankMarketingDashboard.Data;
using BankMarketingDashboard.Models;
using System.Globalization;
using System.Linq;

namespace BankMarketingDashboard.Controllers
{
    /* =========================================================================
     *
     * Flujo general del programa:
     *   1. Leer filtros de la query string (checkboxes como listas, sliders como mínimos).
     *   2. Construir una consulta IQueryable sobre _context.CampaignData aplicando
     *      los filtros (esto hace la agregación en base de datos cuando es posible).
     *   3. Calcular KPIs básicos (conteos, tasas, promedios).
     *   4. Construir colecciones tipadas para las distintas visualizaciones.
     *   5. Almacenar los resultados en ViewBag y retornar la vista.
     *   
     * ========================================================================= */


    public class DashboardController : Controller
    {
        /* ---------------------------------------------------------------------
         *   Inyecta el contexto de datos (ApplicationDbContext) que contiene
         *   la tabla `CampaignData`. Este contexto se usa para construir las
         *   consultas que alimentan el dashboard.
         * --------------------------------------------------------------------- */
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        /* ---------------------------------------------------------------------
         *   Calcula un percentil p (0-100) de una lista ordenada de enteros usando
         *   interpolación lineal entre los valores contiguos. Si la lista está
         *   vacía, devuelve 0.
         * --------------------------------------------------------------------- */
        private static double Percentile(IList<int> sorted, double p)
        {
            if (sorted == null || sorted.Count == 0) return 0;
            var n = sorted.Count;

            // Posición real en la distribución [0, n-1]
            var pos = (p / 100.0) * (n - 1);

            // Índices inferiores y superiores para interpolar
            var lower = (int)System.Math.Floor(pos);
            var upper = (int)System.Math.Ceiling(pos);

            // Si cae exactamente en un índice entero, devolvemos el valor
            if (lower == upper) return sorted[lower];

            // Peso entre lower y upper
            var weight = pos - lower;

            // Interpolación lineal: value = lower*(1-weight) + upper*weight
            return sorted[lower] * (1 - weight) + sorted[upper] * weight;
        }

        /* ---------------------------------------------------------------------
         *   Acción principal que responde a la ruta del dashboard. Lee filtros
         *   (checkboxes repetidos y sliders numéricos), aplica los filtros a la
         *   consulta, calcula KPIs y prepara estructuras tipadas para la vista.
         * --------------------------------------------------------------------- */
        public IActionResult Index()
        {
            // Helper local para leer parámetros tipo "checkbox" que pueden repetirse.
            // Devuelve un arreglo de strings en minúsculas ya recortados; si no existe,
            // devuelve arreglo vacío. Esto facilita chequear "Length > 0" sin nulls.
            string[] GetSelected(string key) =>
                Request.Query.TryGetValue(key, out var v) ? v.Select(s => s?.Trim().ToLowerInvariant()).Where(s => !string.IsNullOrEmpty(s)).ToArray() : Array.Empty<string>();

            // Leer checkboxes (valores categóricos)
            var selMarital = GetSelected("marital");
            var selJob = GetSelected("job");
            var selEducation = GetSelected("education");
            var selDefault = GetSelected("default");
            var selHousing = GetSelected("housing");
            var selLoan = GetSelected("loan");
            var selContact = GetSelected("contact");
            var selMonth = GetSelected("month");
            var selDay = GetSelected("day");
            var selPoutcome = GetSelected("poutcome");

            // Leer sliders numéricos (umbral mínimo). Parse con TryParse para evitar excepciones.
            // Si no se proporcionan, las variables quedan en 0 (valor por defecto).
            int.TryParse(Request.Query["age_min"].FirstOrDefault(), out var ageMin);
            int.TryParse(Request.Query["duration_min"].FirstOrDefault(), out var durationMin);
            int.TryParse(Request.Query["campaign_min"].FirstOrDefault(), out var campaignMin);
            int.TryParse(Request.Query["pdays_min"].FirstOrDefault(), out var pdaysMin);
            int.TryParse(Request.Query["previous_min"].FirstOrDefault(), out var previousMin);

            // Valores double usan InvariantCulture para aceptar puntos como separador decimal.
            double.TryParse(Request.Query["empvarrate_min"].FirstOrDefault(), NumberStyles.Any, CultureInfo.InvariantCulture, out var empVarRateMin);
            double.TryParse(Request.Query["conspriceidx_min"].FirstOrDefault(), NumberStyles.Any, CultureInfo.InvariantCulture, out var consPriceIdxMin);
            double.TryParse(Request.Query["consconfidx_min"].FirstOrDefault(), NumberStyles.Any, CultureInfo.InvariantCulture, out var consConfIdxMin);
            double.TryParse(Request.Query["euribor3m_min"].FirstOrDefault(), NumberStyles.Any, CultureInfo.InvariantCulture, out var euribor3mMin);
            double.TryParse(Request.Query["nremployed_min"].FirstOrDefault(), NumberStyles.Any, CultureInfo.InvariantCulture, out var nrEmployedMin);

            // -----------------------------------------------------------------
            // Base query: empezamos con todos los registros y aplicamos filtros
            // sobre IQueryable para que la base de datos haga el trabajo pesado.
            // -----------------------------------------------------------------
            IQueryable<CampaignRecord> data = _context.CampaignData;

            // Filtrado por valores categóricos únicamente cuando el usuario selecciona opciones.
            // Uso de ToLower para comparar de forma case-insensitive con los valores en sel*.
            if (selMarital.Length > 0) data = data.Where(r => selMarital.Contains((r.Marital ?? "unknown").ToLower()));
            if (selJob.Length > 0) data = data.Where(r => selJob.Contains((r.Job ?? "unknown").ToLower()));
            if (selEducation.Length > 0) data = data.Where(r => selEducation.Contains((r.Education ?? "unknown").ToLower()));
            if (selDefault.Length > 0) data = data.Where(r => selDefault.Contains((r.Default ?? "unknown").ToLower()));
            if (selHousing.Length > 0) data = data.Where(r => selHousing.Contains((r.Housing ?? "unknown").ToLower()));
            if (selLoan.Length > 0) data = data.Where(r => selLoan.Contains((r.Loan ?? "unknown").ToLower()));
            if (selContact.Length > 0) data = data.Where(r => selContact.Contains((r.Contact ?? "unknown").ToLower()));
            if (selMonth.Length > 0) data = data.Where(r => selMonth.Contains((r.Month ?? "unknown").ToLower()));
            if (selDay.Length > 0) data = data.Where(r => selDay.Contains((r.DayOfWeek ?? "unknown").ToLower()));
            if (selPoutcome.Length > 0) data = data.Where(r => selPoutcome.Contains((r.Poutcome ?? "unknown").ToLower()));

            // Aplicar umbrales numéricos únicamente si el usuario proporcionó un mínimo
            // distinto de 0. Esto permite que los sliders empiecen en el mínimo posible.
            if (ageMin > 0) data = data.Where(r => r.Age >= ageMin);
            if (durationMin > 0) data = data.Where(r => r.Duration >= durationMin);
            if (campaignMin > 0) data = data.Where(r => r.Campaign >= campaignMin);
            if (pdaysMin > 0) data = data.Where(r => r.Pdays >= pdaysMin);
            if (previousMin > 0) data = data.Where(r => r.Previous >= previousMin);

            // Para doubles, se comprueba NaN y que no sean 0.0 (valor por defecto)
            if (!double.IsNaN(empVarRateMin) && empVarRateMin != 0.0) data = data.Where(r => r.EmpVarRate >= empVarRateMin);
            if (!double.IsNaN(consPriceIdxMin) && consPriceIdxMin != 0.0) data = data.Where(r => r.ConsPriceIdx >= consPriceIdxMin);
            if (!double.IsNaN(consConfIdxMin) && consConfIdxMin != 0.0) data = data.Where(r => r.ConsConfIdx >= consConfIdxMin);
            if (!double.IsNaN(euribor3mMin) && euribor3mMin != 0.0) data = data.Where(r => r.Euribor3m >= euribor3mMin);
            if (!double.IsNaN(nrEmployedMin) && nrEmployedMin != 0.0) data = data.Where(r => r.NrEmployed >= nrEmployedMin);

            // === KPIs básicos ===
            // Nota: Count() en IQueryable se traduce a COUNT en SQL, eficiente.
            var totalRecords = data.Count();
            var convertedCount = data.Count(r => r.Y == "yes");
            var conversionRate = totalRecords > 0 ? (convertedCount * 100.0 / totalRecords) : 0;
            var avgDuration = data.Any() ? data.Average(r => r.Duration) : 0;

            // === Datos agregados para gráficos ===
            // Agrupaciones típicas: marital, job, education, contact.
            // Cada Select crea un ChartPoint { Label, Value } para consumo por la vista.
            var maritalData = data
                .GroupBy(r => r.Marital)
                .Select(g => new ChartPoint { Label = g.Key ?? "Unknown", Value = g.Count() })
                .ToList();

            var jobData = data
                .GroupBy(r => r.Job)
                .Select(g => new ChartPoint { Label = g.Key ?? "Unknown", Value = g.Count() })
                .ToList();

            var educationData = data
                .GroupBy(r => r.Education)
                .Select(g => new ChartPoint { Label = g.Key ?? "Unknown", Value = g.Count() })
                .ToList();

            var contactData = data
                .GroupBy(r => r.Contact)
                .Select(g => new ChartPoint { Label = g.Key ?? "Unknown", Value = g.Count() })
                .ToList();

            // Comparativas simples para default, housing, loan
            var defaultData = data
                .GroupBy(r => r.Default)
                .Select(g => new ChartPoint { Label = g.Key ?? "Unknown", Value = g.Count() })
                .ToList();

            var housingData = data
                .GroupBy(r => r.Housing)
                .Select(g => new ChartPoint { Label = g.Key ?? "Unknown", Value = g.Count() })
                .ToList();

            var loanData = data
                .GroupBy(r => r.Loan)
                .Select(g => new ChartPoint { Label = g.Key ?? "Unknown", Value = g.Count() })
                .ToList();

            // Conversión por mes: se respeta un orden fijo para presentar la serie temporal
            var monthsOrder = new[] { "jan","feb","mar","apr","may","jun","jul","aug","sep","oct","nov","dec" };
            var monthConversion = monthsOrder.Select(m =>
            {
                // Se cuentan totales y conversiones por mes y se calcula tasa redondeada.
                var total = data.Count(r => r.Month == m);
                var conv = total > 0 ? data.Count(r => r.Month == m && r.Y == "yes") : 0;
                var rate = total > 0 ? Math.Round(conv * 100.0 / total, 2) : 0.0;
                return new ChartPoint { Label = m, Value = rate };
            }).ToList();

            // Conversión por día de la semana (se consideran solo días laborales)
            var daysOrder = new[] { "mon", "tue", "wed", "thu", "fri" };
            var dayConversion = daysOrder.Select(d =>
            {
                var total = data.Count(r => r.DayOfWeek == d);
                var conv = total > 0 ? data.Count(r => r.DayOfWeek == d && r.Y == "yes") : 0;
                var rate = total > 0 ? Math.Round(conv * 100.0 / total, 2) : 0.0;
                return new ChartPoint { Label = d, Value = rate };
            }).ToList();

            // --- Boxplot: separamos duraciones por resultado (sí/no) y calculamos percentiles ---
            var durationsYes = data.Where(r => r.Y == "yes").Select(r => r.Duration).OrderBy(v => v).ToList();
            var durationsNo = data.Where(r => r.Y != "yes").Select(r => r.Duration).OrderBy(v => v).ToList();

            var boxplotData = new List<BoxplotPoint>();
            foreach (var pair in new[] { new { Label = "yes", Values = durationsYes }, new { Label = "no", Values = durationsNo } })
            {
                var vals = pair.Values;
                if (vals == null || vals.Count == 0)
                {
                    // Si no hay valores, añadimos un BoxplotPoint vacío con ceros para evitar nulos en la vista.
                    boxplotData.Add(new BoxplotPoint { Label = pair.Label, Min = 0, Q1 = 0, Median = 0, Q3 = 0, Max = 0 });
                    continue;
                }

                // min/max reales después de ordenar
                var min = vals.First();
                var max = vals.Last();

                // percentiles mediante la función Percentile (usa interpolación)
                var q1 = Percentile(vals, 25);
                var median = Percentile(vals, 50);
                var q3 = Percentile(vals, 75);

                boxplotData.Add(new BoxplotPoint
                {
                    Label = pair.Label,
                    Min = q1 < min ? min : min, // se deja el min original
                    Q1 = q1,
                    Median = median,
                    Q3 = q3,
                    Max = max
                });
            }

            // Agrupación para 'poutcome' con recuentos de sí/no
            var poutcomeGroups = data
                .GroupBy(r => r.Poutcome)
                .Select(g => new PoutcomePoint
                {
                    Label = g.Key ?? "unknown",
                    Yes = g.Count(r => r.Y == "yes"),
                    No = g.Count(r => r.Y != "yes")
                })
                .ToList();

            // Agrupación por número de campaña con tasa de conversión y total
            var campaignGroups = data
                .GroupBy(r => r.Campaign)
                .OrderBy(g => g.Key)
                .Select(g => new CampaignPoint
                {
                    Campaign = g.Key,
                    ConversionRate = g.Count() > 0 ? Math.Round(g.Count(r => r.Y == "yes") * 100.0 / g.Count(), 2) : 0.0,
                    Total = g.Count()
                })
                .ToList();

            // Definición de rangos de edad para representar distribuciones y tasas por grupo
            var ageRanges = new[]
            {
                new { Min = 17, Max = 30, Label = "17-30" },
                new { Min = 31, Max = 45, Label = "31-45" },
                new { Min = 46, Max = 60, Label = "46-60" },
                new { Min = 61, Max = 98, Label = "61-98" }
            };
            var ageData = ageRanges.Select(range => new ChartPoint
            {
                Label = range.Label,
                Value = data.Count(r => r.Age >= range.Min && r.Age <= range.Max)
            }).ToList();

            var conversionByAge = ageRanges.Select(range =>
            {
                var totalInRange = data.Count(r => r.Age >= range.Min && r.Age <= range.Max);
                var convertedInRange = data.Count(r => r.Age >= range.Min && r.Age <= range.Max && r.Y == "yes");
                var value = totalInRange > 0 ? Math.Round(convertedInRange * 100.0 / totalInRange, 2) : 0.0;
                return new ChartPoint { Label = range.Label, Value = value };
            }).ToList();

            // -----------------------------------------------------------------
            // Pasa los resultados a la vista mediante ViewBag.
            // -----------------------------------------------------------------
            ViewBag.TotalContacts = totalRecords;
            ViewBag.ConversionRate = Math.Round(conversionRate, 2);
            ViewBag.AvgDuration = Math.Round(avgDuration, 2);
            ViewBag.SuccessfulCalls = convertedCount;

            ViewBag.MaritalData = maritalData;
            ViewBag.JobData = jobData;
            ViewBag.EducationData = educationData;
            ViewBag.ContactData = contactData;
            ViewBag.AgeData = ageData;
            ViewBag.ConversionByAge = conversionByAge;

            ViewBag.DefaultData = defaultData;
            ViewBag.HousingData = housingData;
            ViewBag.LoanData = loanData;
            ViewBag.MonthConversion = monthConversion;
            ViewBag.DayConversion = dayConversion;
            ViewBag.BoxplotDuration = boxplotData;
            ViewBag.PoutcomeStack = poutcomeGroups;
            ViewBag.CampaignScatter = campaignGroups;

            // Retornamos la vista por defecto del controlador (Index.cshtml).
            return View();
        }
    }
}