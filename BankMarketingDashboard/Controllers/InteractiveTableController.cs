using Microsoft.AspNetCore.Mvc;
using BankMarketingDashboard.Data;
using BankMarketingDashboard.Models;

namespace BankMarketingDashboard.Controllers
{
    public class InteractiveTableController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const int PageSize = 100; // 200 registros por página

        public InteractiveTableController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int page = 1)
        {
            // Validar página
            if (page < 1) page = 1;

            // Total de registros
            var totalRecords = _context.CampaignData.Count();
            var totalPages = (int)Math.Ceiling((double)totalRecords / PageSize);

            // Datos para la página actual
            var records = _context.CampaignData
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // Pasar datos a la vista
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalRecords = totalRecords;

            return View(records);
        }
    }
}