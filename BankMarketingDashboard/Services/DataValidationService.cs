using BankMarketingDashboard.Models;
using System.Globalization;
using System.Text;

namespace BankMarketingDashboard.Services
{
    /* =========================================================================
     *   Servicio responsable de realizar una validación rápida y ligera de
     *   archivos CSV. Escanea un número limitado de filas (muestra) para:
     *     - detectar número de columnas,
     *     - contar filas y duplicados,
     *     - recolectar estadísticas por columna (nulos, tipos inferidos, valores distintos)
     *     - conservar algunas filas de ejemplo para inspección.
     * ========================================================================= */

    /* ---------------------------------------------------------------------
     *   - Diseñada para inyección en controladores y uso durante uploads.
     * --------------------------------------------------------------------- */
    public class DataValidationService
    {
        // Límite de filas a analizar en detalle para controlar tiempo/memoria.
        private const int SampleRowsLimit = 5000; 

        /* ---------------------------------------------------------------------
         *   Lee y analiza un CSV desde un Stream. Devuelve un DataQualityReport
         *   con métricas, muestras y errores (si se producen). No lanza excepciones.
         * --------------------------------------------------------------------- */
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
                // StreamReader con posibilidad de detectar BOM — evita problemas de codificación.
                using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: true);

                // Leemos la primera línea no vacía (cabecera). Si no hay, devolvemos error amigable.
                string headerLine = await ReadNonEmptyLineAsync(reader);
                if (headerLine == null)
                {
                    report.Error = "Empty file or no header line found.";
                    return report;
                }

                // Parseamos cabecera y preparamos ColumnStats para cada columna
                var headers = ParseCsvLine(headerLine).ToArray();
                report.ColumnCount = headers.Length;
                for (int i = 0; i < headers.Length; i++)
                    report.Columns.Add(new ColumnStats { Name = string.IsNullOrWhiteSpace(headers[i]) ? $"Column{i+1}" : headers[i].Trim() });

                var seenRows = new HashSet<string>(); // para detectar duplicados
                string line;
                var rowIndex = 0;

                // Iteramos línea a línea hasta SampleRowsLimit
                while ((line = await reader.ReadLineAsync()) != null && rowIndex < SampleRowsLimit)
                {
                    // Saltar líneas vacías para mantener métricas limpias
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var fields = ParseCsvLine(line).ToArray();
                    report.RowCount++;
                    rowIndex++;

                    // Si una fila tiene menos campos que la cabecera, la rellenamos con nulls
                    if (fields.Length < report.ColumnCount)
                    {
                        Array.Resize(ref fields, report.ColumnCount);
                    }

                    // Guardamos unas pocas filas de muestra para inspección en la UI
                    if (report.SampleRecords.Count < 10)
                        report.SampleRecords.Add(fields);

                    // Comprobación de duplicados: usamos un separador poco probable para construir clave
                    var rowKey = string.Join("\u001F", fields.Select(f => f ?? string.Empty));
                    if (!seenRows.Add(rowKey)) report.DuplicateRows++;

                    // Actualizamos estadísticas por columna
                    for (int c = 0; c < report.ColumnCount; c++)
                    {
                        var val = c < fields.Length ? fields[c] : null;
                        var col = report.Columns[c];

                        // Tratar cadenas vacías/whitespace como nulos
                        if (string.IsNullOrWhiteSpace(val))
                        {
                            col.NullCount++;
                            continue;
                        }

                        // Valor no nulo: contabilizamos y guardamos muestras
                        col.NonNullCount++;
                        col.SampleValues ??= new List<string>();
                        if (col.SampleValues.Count < 10) col.SampleValues.Add(val);

                        // Intentos de clasificación simple: numeric -> date -> string
                        if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                            col.NumericCount++;
                        else if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _))
                            col.DateCount++;
                        else
                            col.StringCount++;

                        // Acumulamos valores distintos hasta un tope para controlar uso de memoria
                        if (col.DistinctValues == null) col.DistinctValues = new HashSet<string>();
                        if (col.DistinctValues.Count <= 1000) col.DistinctValues.Add(val);
                    }
                }

                // Finalizamos la información por columna: número de distintos e inferencia de tipo
                foreach (var col in report.Columns)
                {
                    col.DistinctCount = col.DistinctValues?.Count ?? 0;
                    col.InferredType = InferColumnType(col);
                }

                // Si llegamos al límite de muestra, seguimos leyendo sólo para contar filas totales
                if (report.RowCount >= SampleRowsLimit)
                {
                    while (await reader.ReadLineAsync() != null)
                    {
                        // No procesamos contenido adicional, sólo contamos (evitar memoria extra)
                        if (!string.IsNullOrWhiteSpace(line)) 
                        {
                            
                        }
                        report.RowCount++;
                    }
                }

                return report;
            }
            catch (Exception ex)
            {
                // No lanzamos excepción hacia el controlador: devolvemos un informe con Error
                report.Error = $"Validation failed: {ex.GetType().Name}: {ex.Message}";
                return report;
            }
        }

        /* ---------------------------------------------------------------------
         *   Lee líneas de un StreamReader hasta devolver la primera no vacía.
         * --------------------------------------------------------------------- */
        private static async Task<string> ReadNonEmptyLineAsync(StreamReader sr)
        {
            string l;
            while ((l = await sr.ReadLineAsync()) != null)
            {
                if (!string.IsNullOrWhiteSpace(l)) return l;
            }
            return null;
        }

        /* ---------------------------------------------------------------------
         *   Heurística sencilla que decide si una columna es "numeric", "date",
         *   "string" o "empty" basándose en los contadores recogidos.
         * --------------------------------------------------------------------- */
        private static string InferColumnType(ColumnStats col)
        {
            // Si no hay valores no nulos, consideramos la columna vacía
            if (col.NonNullCount == 0) return "empty";

            // Priorizar numeric si es dominante frente a date/string
            if (col.NumericCount > 0 && col.NumericCount >= col.DateCount && col.NumericCount >= col.StringCount) return "numeric";

            // Si predominan fechas, clasificamos como date
            if (col.DateCount > 0 && col.DateCount >= col.StringCount) return "date";

            // Por defecto: texto
            return "string";
        }

        /* ---------------------------------------------------------------------
         *   Parser sencillo de una línea CSV que soporta:
         *     - campos entrecomillados que pueden contener comas,
         *     - escape de comillas mediante doble comilla ("").
         * --------------------------------------------------------------------- */
        private static IEnumerable<string> ParseCsvLine(string line)
        {
            if (line == null) yield break;
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var ch = line[i];

                if (ch == '"')
                {
                    // Si estamos dentro de comillas y viene otra comilla, es una comilla escapada
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++; // saltamos la comilla escapada
                    }
                    else
                    {
                        // Alternamos estado: entrar o salir de campo citado
                        inQuotes = !inQuotes;
                    }
                    continue;
                }

                // Coma separadora sólo si no estamos dentro de un campo citado
                if (ch == ',' && !inQuotes)
                {
                    yield return sb.ToString();
                    sb.Clear();
                    continue;
                }

                sb.Append(ch);
            }

            // Emitimos el último campo (posible cadena vacía)
            yield return sb.ToString();
        }
    }
}