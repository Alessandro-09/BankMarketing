using Microsoft.AspNetCore.Mvc;
using BankMarketingDashboard.Data;
using BankMarketingDashboard.Models;
using System.Linq;

namespace BankMarketingDashboard.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper para percentiles (interpolación simple)
        private static double Percentile(IList<int> sorted, double p)
        {
            if (sorted == null || sorted.Count == 0) return 0;
            var n = sorted.Count;
            var pos = (p / 100.0) * (n - 1);
            var lower = (int)System.Math.Floor(pos);
            var upper = (int)System.Math.Ceiling(pos);
            if (lower == upper) return sorted[lower];
            var weight = pos - lower;
            return sorted[lower] * (1 - weight) + sorted[upper] * weight;
        }

        public IActionResult Index()
        {
            // === KPIs ===
            var totalRecords = _context.CampaignData.Count();
            var convertedCount = _context.CampaignData.Count(r => r.Y == "yes");
            var conversionRate = totalRecords > 0 ? (convertedCount * 100.0 / totalRecords) : 0;
            var avgDuration = _context.CampaignData.Any() ? _context.CampaignData.Average(r => r.Duration) : 0;

            // === Datos básicos para gráficos ===
            var maritalData = _context.CampaignData
                .GroupBy(r => r.Marital)
                .Select(g => new ChartPoint { Label = g.Key ?? "Unknown", Value = g.Count() })
                .ToList();

            var jobData = _context.CampaignData
                .GroupBy(r => r.Job)
                .Select(g => new ChartPoint { Label = g.Key ?? "Unknown", Value = g.Count() })
                .ToList();

            var educationData = _context.CampaignData
                .GroupBy(r => r.Education)
                .Select(g => new ChartPoint { Label = g.Key ?? "Unknown", Value = g.Count() })
                .ToList();

            var contactData = _context.CampaignData
                .GroupBy(r => r.Contact)
                .Select(g => new ChartPoint { Label = g.Key ?? "Unknown", Value = g.Count() })
                .ToList();

            // === Nuevos gráficos solicitados (RF-2) ===

            // 1-3: barras comparativas para default, housing, loan
            var defaultData = _context.CampaignData
                .GroupBy(r => r.Default)
                .Select(g => new ChartPoint { Label = g.Key ?? "Unknown", Value = g.Count() })
                .ToList();

            var housingData = _context.CampaignData
                .GroupBy(r => r.Housing)
                .Select(g => new ChartPoint { Label = g.Key ?? "Unknown", Value = g.Count() })
                .ToList();

            var loanData = _context.CampaignData
                .GroupBy(r => r.Loan)
                .Select(g => new ChartPoint { Label = g.Key ?? "Unknown", Value = g.Count() })
                .ToList();

            // 4: month vs conversion rate (orden por meses calendario)
            var monthsOrder = new[] { "jan","feb","mar","apr","may","jun","jul","aug","sep","oct","nov","dec" };
            var monthConversion = monthsOrder.Select(m =>
            {
                var total = _context.CampaignData.Count(r => r.Month == m);
                var conv = total > 0 ? _context.CampaignData.Count(r => r.Month == m && r.Y == "yes") : 0;
                var rate = total > 0 ? Math.Round(conv * 100.0 / total, 2) : 0.0;
                return new ChartPoint { Label = m, Value = rate };
            }).ToList();

            // 5: heatmap day_of_week vs conversion rate (dias Mon-Fri)
            var daysOrder = new[] { "mon", "tue", "wed", "thu", "fri" };
            var dayConversion = daysOrder.Select(d =>
            {
                var total = _context.CampaignData.Count(r => r.DayOfWeek == d);
                var conv = total > 0 ? _context.CampaignData.Count(r => r.DayOfWeek == d && r.Y == "yes") : 0;
                var rate = total > 0 ? Math.Round(conv * 100.0 / total, 2) : 0.0;
                return new ChartPoint { Label = d, Value = rate };
            }).ToList();

            // 6: boxplot duration comparing y == "yes" vs y == "no"
            var durationsYes = _context.CampaignData.Where(r => r.Y == "yes").Select(r => r.Duration).ToList();
            var durationsNo = _context.CampaignData.Where(r => r.Y != "yes").Select(r => r.Duration).ToList();

            List<object> boxplotData = new List<object>();
            foreach (var pair in new[] { new { Label = "yes", Values = durationsYes }, new { Label = "no", Values = durationsNo } })
            {
                var vals = pair.Values.OrderBy(v => v).ToList();
                if (vals.Count == 0)
                {
                    boxplotData.Add(new { label = pair.Label, min = 0, q1 = 0, median = 0, q3 = 0, max = 0 });
                    continue;
                }
                var min = vals.First();
                var max = vals.Last();
                var q1 = Percentile(vals, 25);
                var median = Percentile(vals, 50);
                var q3 = Percentile(vals, 75);
                boxplotData.Add(new { label = pair.Label, min, q1, median, q3, max });
            }

            // 7: barras apiladas poutcome vs y
            var poutcomeGroups = _context.CampaignData
                .GroupBy(r => r.Poutcome)
                .Select(g => new
                {
                    Label = g.Key ?? "unknown",
                    Yes = g.Count(r => r.Y == "yes"),
                    No = g.Count(r => r.Y != "yes")
                })
                .ToList();

            // 8: scatter campaign vs conversion rate
            var campaignGroups = _context.CampaignData
                .GroupBy(r => r.Campaign)
                .OrderBy(g => g.Key)
                .Select(g =>
                    new
                    {
                        Campaign = g.Key,
                        ConversionRate = g.Count() > 0 ? Math.Round(g.Count(r => r.Y == "yes") * 100.0 / g.Count(), 2) : 0.0,
                        Total = g.Count()
                    }
                ).ToList();

            // Distribución de edades (RF-2)
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
                Value = _context.CampaignData.Count(r => r.Age >= range.Min && r.Age <= range.Max)
            }).ToList();

            var conversionByAge = ageRanges.Select(range =>
            {
                var totalInRange = _context.CampaignData.Count(r => r.Age >= range.Min && r.Age <= range.Max);
                var convertedInRange = _context.CampaignData.Count(r => r.Age >= range.Min && r.Age <= range.Max && r.Y == "yes");
                var value = totalInRange > 0 ? Math.Round(convertedInRange * 100.0 / totalInRange, 2) : 0.0;
                return new ChartPoint { Label = range.Label, Value = value };
            }).ToList();

            // Pasar todo a la vista (ViewBag)
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
            ViewBag.BoxplotDuration = boxplotData;          // lista de objetos con min,q1,median,q3,max
            ViewBag.PoutcomeStack = poutcomeGroups;        // lista con Label,Yes,No
            ViewBag.CampaignScatter = campaignGroups;      // lista con Campaign, ConversionRate, Total

            return View();
        }
    }
}
