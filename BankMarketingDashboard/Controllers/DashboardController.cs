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

        public IActionResult Index()
        {
            // === KPIs ===
            var totalRecords = _context.CampaignData.Count();
            var convertedCount = _context.CampaignData.Count(r => r.Y == "yes");
            var conversionRate = totalRecords > 0 ? (convertedCount * 100.0 / totalRecords) : 0;
            var avgDuration = _context.CampaignData.Any() ? _context.CampaignData.Average(r => r.Duration) : 0;

            // === Datos para gráficos usando ChartPoint (tipo concreto) ===
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
                var value = totalInRange > 0 ? (convertedInRange * 100.0 / totalInRange) : 0.0;
                return new ChartPoint { Label = range.Label, Value = Math.Round(value, 2) };
            }).ToList();

            // Pasar todo a la vista
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

            return View();
        }
    }
}
