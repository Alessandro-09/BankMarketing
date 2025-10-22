namespace BankMarketingDashboard.Models
{
    public class DataQualityReport
    {
        public string FileName { get; set; }
        public DateTime DetectedAt { get; set; }
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
        public List<ColumnStats> Columns { get; set; } = new();
        public int DuplicateRows { get; set; }
        public List<string[]> SampleRecords { get; set; } = new();
        public string Error { get; set; }
    }

    public class ColumnStats
    {
        public string Name { get; set; }
        public int NullCount { get; set; }
        public int NonNullCount { get; set; }
        public int NumericCount { get; set; }
        public int DateCount { get; set; }
        public int StringCount { get; set; }
        public int DistinctCount { get; set; }
        public HashSet<string> DistinctValues { get; set; }
        public List<string> SampleValues { get; set; }
        public string InferredType { get; set; }
    }
}