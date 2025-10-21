namespace BankMarketingDashboard.Models
{
    public class BoxplotPoint
    {
        public string Label { get; set; } = string.Empty;
        public double Min { get; set; }
        public double Q1 { get; set; }
        public double Median { get; set; }
        public double Q3 { get; set; }
        public double Max { get; set; }
    }
}