namespace BankMarketingDashboard.Models
{
    public class CampaignRecord
    {
        public int Age { get; set; }
        public string Job { get; set; } = string.Empty;
        public string Marital { get; set; } = string.Empty;
        public string Education { get; set; } = string.Empty;
        public string Default { get; set; } = string.Empty;
        public string Housing { get; set; } = string.Empty;
        public string Loan { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string Month { get; set; } = string.Empty;
        public string DayOfWeek { get; set; } = string.Empty;
        public int Duration { get; set; }
        public int Campaign { get; set; }
        public int Pdays { get; set; }
        public int Previous { get; set; }
        public string Poutcome { get; set; } = string.Empty;
        public double EmpVarRate { get; set; }        
        public double ConsPriceIdx { get; set; }      
        public double ConsConfIdx { get; set; }       
        public double Euribor3m { get; set; }         
        public double NrEmployed { get; set; }        
        public string Y { get; set; } = string.Empty;
    }
}