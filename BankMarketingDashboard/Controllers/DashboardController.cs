using Microsoft.AspNetCore.Mvc;
using BankMarketingDashboard.Data;
using BankMarketingDashboard.Models;

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
            // primeros 10 registros para probar conexión
            var records = _context.CampaignData.Take(10).ToList();
            return View(records);
        }
    }
}