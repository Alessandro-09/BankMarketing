using BankMarketingDashboard.Data;
using BankMarketingDashboard.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registrar servicios en el contenedor de dependencias (DI).
builder.Services.AddControllersWithViews();

// Registrar el servicio de validación de datos para que los controladores lo reciban vía DI.
builder.Services.AddScoped<DataValidationService>();

// Aumentar el tamaño máximo permitido para cargas multipart (ej.: 200 MB).
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 200 * 1024 * 1024; // 200 MB
});

// Opcional: configurar límite máximo del cuerpo de la petición en Kestrel.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 200 * 1024 * 1024; // 200 MB
});

var app = builder.Build();

// Obtener un logger para los manejadores globales.
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Manejadores globales para registrar excepciones inesperadas a nivel de proceso.
// Útil para capturar errores que no llegan al pipeline normal de ASP.NET Core.
AppDomain.CurrentDomain.UnhandledException += (s, e) =>
{
    logger.LogCritical(e.ExceptionObject as Exception, "Unhandled exception (AppDomain)");
};
TaskScheduler.UnobservedTaskException += (s, e) =>
{
    logger.LogCritical(e.Exception, "Unobserved task exception");
    e.SetObserved();
};

// Configurar el pipeline HTTP.
if (app.Environment.IsDevelopment())
{
    // Mostrar la página de desarrollador en Development para ver las pilas de llamadas en el navegador.
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
