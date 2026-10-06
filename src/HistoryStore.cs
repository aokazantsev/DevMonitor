using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace DevMonitor
{
    internal sealed class HistoryStore
    {
        public const int RetainedFullWeeks = 2;

        private const string FileDateFormat = "yyyy-MM-dd";
        private const string TimeFormat = "HH:mm";
        private const char Separator = ',';
        private const int MissingColumn = -1;

        private static readonly Encoding FileEncoding = new UTF8Encoding(false);

        private readonly string directory = UserDataPaths.History;
        private string currentHeaderPath;

        public void Append(MinuteRecord record)
        {
            try
            {
                Directory.CreateDirectory(directory);
                DateTime day = record.Time.Date;
                string path = FileFor(day);
                if (path != currentHeaderPath)
                {
                    UpgradeHeader(path, day);
                    currentHeaderPath = path;
                }
                bool isNewFile = !File.Exists(path);
                using (var writer = new StreamWriter(path, true, FileEncoding))
                {
                    if (isNewFile) writer.WriteLine(Header());
                    writer.WriteLine(Line(record));
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        public void Purge(DateTime today)
        {
            DateTime oldestKept = OldestKeptDay(today);
            try
            {
                if (!Directory.Exists(directory)) return;
                foreach (string path in Directory.GetFiles(directory, "*.csv"))
                {
                    DateTime day;
                    if (TryParseDay(path, out day) && day < oldestKept) File.Delete(path);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        public List<MinuteRecord> Load(DateTime firstDay, int days)
        {
            var records = new List<MinuteRecord>();
            for (int offset = 0; offset < days; offset++)
            {
                DateTime day = firstDay.AddDays(offset);
                string path = FileFor(day);
                if (!File.Exists(path)) continue;
                try
                {
                    records.AddRange(ReadDay(path, day));
                }
                catch (IOException)
                {
                }
            }
            return records;
        }

        public static DateTime OldestKeptDay(DateTime today)
        {
            return WeekDays.WeekStart(today).AddDays(-7 * RetainedFullWeeks);
        }

        private void UpgradeHeader(string path, DateTime day)
        {
            if (!File.Exists(path)) return;
            string header;
            using (var reader = new StreamReader(path, Encoding.UTF8))
            {
                header = reader.ReadLine();
            }
            if (header == Header()) return;
            var lines = new List<string> { Header() };
            foreach (MinuteRecord record in ReadDay(path, day)) lines.Add(Line(record));
            string temporaryPath = path + ".tmp";
            File.WriteAllLines(temporaryPath, lines, FileEncoding);
            File.Replace(temporaryPath, path, null);
        }

        private static List<MinuteRecord> ReadDay(string path, DateTime day)
        {
            var records = new List<MinuteRecord>();
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length == 0) return records;
            string[] header = lines[0].Split(Separator);
            int[] averageColumns = ColumnsOf(header, "_avg");
            int[] maximumColumns = ColumnsOf(header, "_max");
            for (int index = 1; index < lines.Length; index++)
            {
                MinuteRecord record;
                if (TryParseLine(day, lines[index], header.Length, averageColumns, maximumColumns, out record)) records.Add(record);
            }
            return records;
        }

        private static int[] ColumnsOf(string[] header, string suffix)
        {
            var columns = new int[MetricCatalog.Count];
            for (int index = 0; index < MetricCatalog.Count; index++)
            {
                columns[index] = Array.IndexOf(header, MetricCatalog.CsvKey(MetricCatalog.All[index]) + suffix);
            }
            return columns;
        }

        private string FileFor(DateTime day)
        {
            return Path.Combine(directory, day.ToString(FileDateFormat, CultureInfo.InvariantCulture) + ".csv");
        }

        private static bool TryParseDay(string path, out DateTime day)
        {
            return DateTime.TryParseExact(Path.GetFileNameWithoutExtension(path), FileDateFormat,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out day);
        }

        private static string Header()
        {
            var builder = new StringBuilder("time");
            foreach (MetricKind kind in MetricCatalog.All)
            {
                string key = MetricCatalog.CsvKey(kind);
                builder.Append(Separator).Append(key).Append("_avg").Append(Separator).Append(key).Append("_max");
            }
            return builder.ToString();
        }

        private static string Line(MinuteRecord record)
        {
            var builder = new StringBuilder(record.Time.ToString(TimeFormat, CultureInfo.InvariantCulture));
            for (int index = 0; index < MetricCatalog.Count; index++)
            {
                builder.Append(Separator).Append(FormatValue(record.Average[index]));
                builder.Append(Separator).Append(FormatValue(record.Maximum[index]));
            }
            return builder.ToString();
        }

        private static string FormatValue(float? value)
        {
            return value.HasValue ? value.Value.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty;
        }

        private static bool TryParseLine(DateTime day, string line, int columnCount, int[] averageColumns, int[] maximumColumns, out MinuteRecord record)
        {
            record = null;
            string[] parts = line.Split(Separator);
            if (parts.Length != columnCount) return false;
            DateTime time;
            if (!DateTime.TryParseExact(parts[0], TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out time)) return false;
            var average = new float?[MetricCatalog.Count];
            var maximum = new float?[MetricCatalog.Count];
            for (int index = 0; index < MetricCatalog.Count; index++)
            {
                average[index] = ValueAt(parts, averageColumns[index]);
                maximum[index] = ValueAt(parts, maximumColumns[index]);
            }
            record = new MinuteRecord(day.Add(time.TimeOfDay), average, maximum);
            return true;
        }

        private static float? ValueAt(string[] parts, int column)
        {
            return column == MissingColumn ? null : ParseValue(parts[column]);
        }

        private static float? ParseValue(string text)
        {
            float value;
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : (float?)null;
        }
    }
}
