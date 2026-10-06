using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace DevMonitor
{
    internal static class HardwareCatalog
    {
        private const char Separator = ';';
        private const char CommentMark = '#';
        private const int FieldCount = 5;
        private const string DriverLimits = "auto";

        private static readonly string FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hardware.csv");

        public static List<HardwareProfile> Load()
        {
            var profiles = new List<HardwareProfile>();
            if (!File.Exists(FilePath)) return profiles;
            try
            {
                foreach (string rawLine in File.ReadAllLines(FilePath, Encoding.UTF8))
                {
                    HardwareProfile profile;
                    if (TryParse(rawLine.Trim(), out profile)) profiles.Add(profile);
                }
            }
            catch (IOException)
            {
            }
            return profiles;
        }

        public static List<HardwareProfile> OfKind(List<HardwareProfile> profiles, HardwareKind kind)
        {
            return profiles.FindAll(profile => profile.Kind == kind);
        }

        public static HardwareProfile Find(List<HardwareProfile> profiles, HardwareKind kind, string name)
        {
            return profiles.Find(profile => profile.Kind == kind && string.Equals(profile.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private static bool TryParse(string line, out HardwareProfile profile)
        {
            profile = null;
            if (line.Length == 0 || line[0] == CommentMark) return false;
            string[] fields = line.Split(Separator);
            if (fields.Length != FieldCount) return false;

            HardwareKind kind;
            if (!TryParseKind(fields[0].Trim(), out kind)) return false;
            string name = fields[1].Trim();
            if (name.Length == 0) return false;
            string note = fields[4].Trim();

            if (kind == HardwareKind.Gpu && IsDriverLimits(fields[2]) && IsDriverLimits(fields[3]))
            {
                profile = new HardwareProfile(kind, name, null, note);
                return true;
            }
            float warning, critical;
            if (!TryParseDegrees(fields[2], out warning) || !TryParseDegrees(fields[3], out critical)) return false;
            profile = new HardwareProfile(kind, name, new TemperatureZones(warning, critical), note);
            return true;
        }

        private static bool IsDriverLimits(string text)
        {
            return string.Equals(text.Trim(), DriverLimits, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParseKind(string text, out HardwareKind kind)
        {
            kind = HardwareKind.Cpu;
            if (string.Equals(text, "CPU", StringComparison.OrdinalIgnoreCase)) return true;
            kind = HardwareKind.Gpu;
            return string.Equals(text, "GPU", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParseDegrees(string text, out float value)
        {
            return float.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
