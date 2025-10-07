using Microsoft.EntityFrameworkCore;
using BankMarketingDashboard.Models;

namespace BankMarketingDashboard.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<CampaignRecord> CampaignData { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CampaignRecord>(entity =>
            {
                entity.ToTable("CampaignData");
                entity.HasKey(e => new { e.Age, e.Job, e.Duration, e.Campaign }); // Clave compuesta 

                // Mapeo de propiedades a columnas
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