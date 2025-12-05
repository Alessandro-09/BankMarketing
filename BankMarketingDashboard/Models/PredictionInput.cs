using Microsoft.ML.Data;

namespace BankMarketingDashboard.Models
{
    public class PredictionInput
    {
        [LoadColumn(0)]
        public float Age { get; set; }

        [LoadColumn(1)]
        public string Job { get; set; } = string.Empty;

        [LoadColumn(2)]
        public string Marital { get; set; } = string.Empty;

        [LoadColumn(3)]
        public string Education { get; set; } = string.Empty;

        [LoadColumn(4)]
        public string Default { get; set; } = string.Empty;

        [LoadColumn(5)]
        public string Housing { get; set; } = string.Empty;

        [LoadColumn(6)]
        public string Loan { get; set; } = string.Empty;

        [LoadColumn(7)]
        public string Contact { get; set; } = string.Empty;

        [LoadColumn(8)]
        public string Month { get; set; } = string.Empty;

        [LoadColumn(9)]
        public string DayOfWeek { get; set; } = string.Empty;

        [LoadColumn(10)]
        public float Duration { get; set; }

        [LoadColumn(11)]
        public float Campaign { get; set; }

        [LoadColumn(12)]
        public float Pdays { get; set; }

        [LoadColumn(13)]
        public float Previous { get; set; }

        [LoadColumn(14)]
        public string Poutcome { get; set; } = string.Empty;

        [LoadColumn(15)]
        public float EmpVarRate { get; set; }

        [LoadColumn(16)]
        public float ConsPriceIdx { get; set; }

        [LoadColumn(17)]
        public float ConsConfIdx { get; set; }

        [LoadColumn(18)]
        public float Euribor3m { get; set; }

        [LoadColumn(19)]
        public float NrEmployed { get; set; }

        [LoadColumn(20)]
        public string? Label { get; set; }
    }
}
