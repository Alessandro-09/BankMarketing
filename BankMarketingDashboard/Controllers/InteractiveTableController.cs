// Controllers/InteractiveTableController.cs
using BankMarketingDashboard.Data;
using BankMarketingDashboard.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using ClosedXML.Excel;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace BankMarketingDashboard.Controllers
{
    public class InteractiveTableController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const int PageSize = 100;

        public InteractiveTableController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(
            int page = 1,
            int? minAge = null,
            int? maxAge = null,
            string[]? job = null,
            string[]? marital = null,
            string[]? education = null,
            string[]? month = null,
            string[]? y = null)
        {
            if (page < 1) page = 1;

            var query = BuildFilteredQuery(minAge, maxAge, job, marital, education, month, y);

            var totalRecords = query.Count();
            var totalPages = (int)System.Math.Ceiling((double)totalRecords / PageSize);

            var records = query
                .OrderBy(c => c.Age)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

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

            return View(records);
        }

        [HttpGet]
        public IActionResult TablePartial(
            int page = 1,
            int? minAge = null,
            int? maxAge = null,
            string[]? job = null,
            string[]? marital = null,
            string[]? education = null,
            string[]? month = null,
            string[]? y = null)
        {
            System.Diagnostics.Debug.WriteLine($"TablePartial called: page={page}, minAge={minAge}, maxAge={maxAge}, job={(job==null? "null": string.Join("|",job))}, marital={(marital==null? "null": string.Join("|",marital))}, education={(education==null? "null": string.Join("|",education))}, month={(month==null? "null": string.Join("|",month))}, y={(y==null? "null": string.Join("|",y))}");

            if (page < 1) page = 1;

            var query = BuildFilteredQuery(minAge, maxAge, job, marital, education, month, y);

            var totalRecords = query.Count();
            var totalPages = (int)System.Math.Ceiling((double)totalRecords / PageSize);

            var records = query
                .OrderBy(c => c.Age)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

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

            return PartialView("_InteractiveTablePartial", records);
        }

        // Export CSV for filtered results (query parameters same as Index)
        public IActionResult ExportCsv(
            int? minAge = null,
            int? maxAge = null,
            string[]? job = null,
            string[]? marital = null,
            string[]? education = null,
            string[]? month = null,
            string[]? y = null)
        {
            try
            {
                var query = BuildFilteredQuery(minAge, maxAge, job, marital, education, month, y);
                var records = query.ToList();

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

                var tempPath = Path.Combine(Path.GetTempPath(), $"bankmarketing_{System.Guid.NewGuid()}.csv");
                System.IO.File.WriteAllText(tempPath, "\uFEFF" + csv.ToString(), new UTF8Encoding(true));

                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromMinutes(5));
                    try { System.IO.File.Delete(tempPath); } catch { }
                });

                return PhysicalFile(tempPath, "text/csv; charset=utf-8", "bankmarketing_export_filtered.csv");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Export CSV error: {ex}");
                return StatusCode(500, "CSV export failed.");
            }
        }

        // Export Excel for filtered results (query parameters same as Index)
        public IActionResult ExportExcel(
            int? minAge = null,
            int? maxAge = null,
            string[]? job = null,
            string[]? marital = null,
            string[]? education = null,
            string[]? month = null,
            string[]? y = null)
        {
            try
            {
                var query = BuildFilteredQuery(minAge, maxAge, job, marital, education, month, y);
                var records = query.ToList();

                var tempPath = Path.Combine(Path.GetTempPath(), $"bankmarketing_{System.Guid.NewGuid()}.xlsx");

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Filtered Data");

                    var headers = new[]
                    {
                        "Age", "Job", "Marital", "Education", "Default", "Housing", "Loan", "Contact",
                        "Month", "DayOfWeek", "Duration", "Campaign", "Pdays", "Previous", "Poutcome",
                        "EmpVarRate", "ConsPriceIdx", "ConsConfIdx", "Euribor3m", "NrEmployed", "Y"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = headers[i];
                        worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                        worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#CB3CFF");
                        worksheet.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
                    }

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

                    worksheet.Columns().AdjustToContents();
                    workbook.SaveAs(tempPath);
                }

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

        // Helper to construct filtered IQueryable
        private IQueryable<CampaignRecord> BuildFilteredQuery(
            int? minAge, int? maxAge,
            string[]? job, string[]? marital, string[]? education, string[]? month, string[]? y)
        {
            var query = _context.CampaignData.AsQueryable();

            if (minAge.HasValue) query = query.Where(c => c.Age >= minAge.Value);
            if (maxAge.HasValue) query = query.Where(c => c.Age <= maxAge.Value);

            // Job filter: build server-side OR of EF.Functions.Like((c.Job ?? "").ToLower(), "%value%")
            if (job != null && job.Any(s => !string.IsNullOrWhiteSpace(s)))
            {
                var arr = job.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.ToLower()).ToArray();
                if (arr.Length > 0)
                {
                    var param = System.Linq.Expressions.Expression.Parameter(typeof(CampaignRecord), "c");
                    System.Linq.Expressions.Expression? body = null;

                    var jobProp = System.Linq.Expressions.Expression.Property(param, nameof(CampaignRecord.Job));
                    var coalesce = System.Linq.Expressions.Expression.Coalesce(jobProp, System.Linq.Expressions.Expression.Constant(""));
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

            if (marital != null && marital.Any(s => !string.IsNullOrWhiteSpace(s)))
            {
                var arr = marital.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                query = query.Where(c => arr.Contains((c.Marital ?? "")));
            }

            // Education filter: build OR of EF.Functions.Like((c.Education ?? ""), "%" + j + "%")
            if (education != null && education.Any(s => !string.IsNullOrWhiteSpace(s)))
            {
                var arr = education.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                if (arr.Length > 0)
                {
                    var param = System.Linq.Expressions.Expression.Parameter(typeof(CampaignRecord), "c");
                    System.Linq.Expressions.Expression? body = null;

                    var eduProp = System.Linq.Expressions.Expression.Property(param, nameof(CampaignRecord.Education));
                    var coalesce = System.Linq.Expressions.Expression.Coalesce(eduProp, System.Linq.Expressions.Expression.Constant(""));

                    var efFunctionsProp = typeof(Microsoft.EntityFrameworkCore.EF).GetProperty(nameof(Microsoft.EntityFrameworkCore.EF.Functions))!;
                    var efFunctionsExpr = System.Linq.Expressions.Expression.Property(null, efFunctionsProp);

                    var likeMethod = typeof(Microsoft.EntityFrameworkCore.DbFunctionsExtensions)
                        .GetMethod(nameof(Microsoft.EntityFrameworkCore.DbFunctionsExtensions.Like), new[] { typeof(Microsoft.EntityFrameworkCore.DbFunctions), typeof(string), typeof(string) })!;

                    foreach (var val in arr)
                    {
                        var pattern = System.Linq.Expressions.Expression.Constant("%" + val + "%");
                        var likeCall = System.Linq.Expressions.Expression.Call(null, likeMethod, efFunctionsExpr, coalesce, pattern);
                        body = body == null ? likeCall : System.Linq.Expressions.Expression.OrElse(body, likeCall);
                    }

                    var lambda = System.Linq.Expressions.Expression.Lambda<Func<CampaignRecord, bool>>(body!, param);
                    query = query.Where(lambda);
                }
            }

            if (month != null && month.Any(s => !string.IsNullOrWhiteSpace(s)))
            {
                var arr = month.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                query = query.Where(c => arr.Contains((c.Month ?? "")));
            }

            if (y != null && y.Any(s => !string.IsNullOrWhiteSpace(s)))
            {
                var arr = y.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                query = query.Where(c => arr.Contains((c.Y ?? "")));
            }

            return query;
        }
    }
}