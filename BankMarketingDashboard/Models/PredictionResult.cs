namespace BankMarketingDashboard.Models
{
    public class PredictionResult
    {
        public double Probability { get; set; }
        public string Classification { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool WillAccept { get; set; }
    }
}
