using BankMarketingDashboard.Models;
using System.Globalization;
using System.Text;

namespace BankMarketingDashboard.Services
{
    public class DataValidationService
    {
        private const int SampleRowsLimit = 5000; // limit for validation scanning

        public async Task<DataQualityReport> ValidateCsvAsync(Stream input, string filename = null, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;

            var report = new DataQualityReport
            {
                FileName = filename,
                DetectedAt = DateTime.UtcNow,
                RowCount = 0,
                ColumnCount = 0,
                Columns = new List<ColumnStats>(),
                SampleRecords = new List<string[]>()
            };

            try
            {
                using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: true);

                string headerLine = await ReadNonEmptyLineAsync(reader);
                if (headerLine == null)
                {
                    report.Error = "Empty file or no header line found.";
                    return report;
                }

                var headers = ParseCsvLine(headerLine).ToArray();
                report.ColumnCount = headers.Length;
                for (int i = 0; i < headers.Length; i++)
                    report.Columns.Add(new ColumnStats { Name = string.IsNullOrWhiteSpace(headers[i]) ? $"Column{i+1}" : headers[i].Trim() });

                var seenRows = new HashSet<string>();
                string line;
                var rowIndex = 0;
                while ((line = await reader.ReadLineAsync()) != null && rowIndex < SampleRowsLimit)
                {
                    // skip empty lines
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var fields = ParseCsvLine(line).ToArray();
                    report.RowCount++;
                    rowIndex++;

                    // pad fields if row shorter
                    if (fields.Length < report.ColumnCount)
                    {
                        Array.Resize(ref fields, report.ColumnCount);
                    }

                    // collect sample (first 10)
                    if (report.SampleRecords.Count < 10)
                        report.SampleRecords.Add(fields);

                    // duplicate check
                    var rowKey = string.Join("\u001F", fields.Select(f => f ?? string.Empty));
                    if (!seenRows.Add(rowKey)) report.DuplicateRows++;

                    // per-column stats
                    for (int c = 0; c < report.ColumnCount; c++)
                    {
                        var val = c < fields.Length ? fields[c] : null;
                        var col = report.Columns[c];

                        if (string.IsNullOrWhiteSpace(val))
                        {
                            col.NullCount++;
                            continue;
                        }

                        col.NonNullCount++;
                        col.SampleValues ??= new List<string>();
                        if (col.SampleValues.Count < 10) col.SampleValues.Add(val);

                        // attempt numeric
                        if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                            col.NumericCount++;
                        else if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _))
                            col.DateCount++;
                        else
                            col.StringCount++;

                        // count distinct up to limit
                        if (col.DistinctValues == null) col.DistinctValues = new HashSet<string>();
                        if (col.DistinctValues.Count <= 1000) col.DistinctValues.Add(val);
                    }
                }

                // finalize column types & percentages
                foreach (var col in report.Columns)
                {
                    col.DistinctCount = col.DistinctValues?.Count ?? 0;
                    col.InferredType = InferColumnType(col);
                }

                // if we stopped early due to SampleRowsLimit, try to estimate total rows by continuing to read count only
                if (report.RowCount >= SampleRowsLimit)
                {
                    while (await reader.ReadLineAsync() != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line)) // no-op, just consume; keeping pattern
                        {
                            /* intentionally left blank */
                        }
                        report.RowCount++;
                    }
                }

                return report;
            }
            catch (Exception ex)
            {
                // avoid throwing — return a report with error information so controller can return it safely
                report.Error = $"Validation failed: {ex.GetType().Name}: {ex.Message}";
                return report;
            }
        }

        private static async Task<string> ReadNonEmptyLineAsync(StreamReader sr)
        {
            string l;
            while ((l = await sr.ReadLineAsync()) != null)
            {
                if (!string.IsNullOrWhiteSpace(l)) return l;
            }
            return null;
        }

        private static string InferColumnType(ColumnStats col)
        {
            // simple heuristic
            if (col.NonNullCount == 0) return "empty";
            if (col.NumericCount > 0 && col.NumericCount >= col.DateCount && col.NumericCount >= col.StringCount) return "numeric";
            if (col.DateCount > 0 && col.DateCount >= col.StringCount) return "date";
            return "string";
        }

        // Basic CSV parser that supports quoted fields and double-quote escaping
        private static IEnumerable<string> ParseCsvLine(string line)
        {
            if (line == null) yield break;
            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (ch == '"' )
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // escaped quote
                        sb.Append('"');
                        i++; // skip next
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                    continue;
                }

                if (ch == ',' && !inQuotes)
                {
                    yield return sb.ToString();
                    sb.Clear();
                    continue;
                }

                sb.Append(ch);
            }
            yield return sb.ToString();
        }
    }
}