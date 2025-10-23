using Microsoft.EntityFrameworkCore;
using BankMarketingDashboard.Models;

namespace BankMarketingDashboard.Data
{
    /* =========================================================================
     *   Contexto de Entity Framework Core que representa el acceso a la tabla
     *   `CampaignData` y define el mapeo entre la clase `CampaignRecord` y la
     *   estructura de la base de datos. Contiene la configuración de la tabla
     *   (nombre, clave y mapeo de columnas).
     * ========================================================================= */

    /* ---------------------------------------------------------------------
     *   Clase que hereda de `DbContext` y configura el modelo de datos para
     *   EF Core en esta aplicación.
     * --------------------------------------------------------------------- */
    public class ApplicationDbContext : DbContext
    {
        /* -----------------------------------------------------------------
         *   Inicializa una instancia del contexto con las opciones proporcionadas
         *   por la configuración de la aplicación (conexión, proveedor, etc.).
         * ----------------------------------------------------------------- */
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /* -----------------------------------------------------------------
         *   Expone la tabla `CampaignData` como un `DbSet<CampaignRecord>`.
         * ----------------------------------------------------------------- */
        public DbSet<CampaignRecord> CampaignData { get; set; }

        /* ---------------------------------------------------------------------
         *   Método donde se configura el mapeo entre la clase `CampaignRecord`
         *   y la tabla/columnas de la base de datos. Aquí se declaran nombre de tabla,
         *   clave primaria compuesta y los nombres de columna para cada propiedad.
         * --------------------------------------------------------------------- */
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CampaignRecord>(entity =>
            {
                // Nombre de la tabla en la base de datos
                entity.ToTable("CampaignData");

                // Se declara una clave primaria compuesta. Esto significa que EF Core
                // considerará la combinación de estas columnas como identificador único.
                entity.HasKey(e => new { e.Age, e.Job, e.Duration, e.Campaign });

                // Mapeo explícito de propiedades a columnas — útil cuando los nombres
                // en la clase difieren o se desea controlar el esquema resultante.
                // Se hace propiedad por propiedad para evitar sorpresas con convenciones.
                entity.Property(e => e.Age).HasColumnName("age");
                entity.Property(e => e.Job).HasColumnName("job");
                entity.Property(e => e.Marital).HasColumnName("marital");
                entity.Property(e => e.Education).HasColumnName("education");
                entity.Property(e => e.Default).HasColumnName("default");
                entity.Property(e => e.Housing).HasColumnName("housing");
                entity.Property(e => e.Loan).HasColumnName("loan");
                entity.Property(e => e.Contact).HasColumnName("contact");
                entity.Property(e => e.Month).HasColumnName("month");
                entity.Property(e => e.DayOfWeek).HasColumnName("day_of_week");
                entity.Property(e => e.Duration).HasColumnName("duration");
                entity.Property(e => e.Campaign).HasColumnName("campaign");
                entity.Property(e => e.Pdays).HasColumnName("pdays");
                entity.Property(e => e.Previous).HasColumnName("previous");
                entity.Property(e => e.Poutcome).HasColumnName("poutcome");
                entity.Property(e => e.EmpVarRate).HasColumnName("emp_var_rate");
                entity.Property(e => e.ConsPriceIdx).HasColumnName("cons_price_idx");
                entity.Property(e => e.ConsConfIdx).HasColumnName("cons_conf_idx");
                entity.Property(e => e.Euribor3m).HasColumnName("euribor3m");
                entity.Property(e => e.NrEmployed).HasColumnName("nr_employed");
                entity.Property(e => e.Y).HasColumnName("y");
            });
        }
    }
}