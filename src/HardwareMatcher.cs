using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DevMonitor
{
    internal static class HardwareMatcher
    {
        private static readonly Regex Noise = new Regex(@"\((r|tm)\)|\bprocessor\b|\bcpu\b|@.*$|\d+-core\b|\bwith radeon.*$", RegexOptions.IgnoreCase);
        private static readonly Regex Spaces = new Regex(@"\s+");

        public static HardwareProfile Detect(List<HardwareProfile> catalog, HardwareKind kind, IEnumerable<string> installedNames)
        {
            HardwareProfile best = null;
            int bestLength = 0;
            foreach (string installed in installedNames)
            {
                if (string.IsNullOrEmpty(installed)) continue;
                string padded = " " + Normalize(installed) + " ";
                foreach (HardwareProfile profile in HardwareCatalog.OfKind(catalog, kind))
                {
                    foreach (string variant in Variants(profile.Name))
                    {
                        if (variant.Length <= bestLength) continue;
                        if (padded.IndexOf(" " + variant + " ", StringComparison.Ordinal) >= 0)
                        {
                            best = profile;
                            bestLength = variant.Length;
                        }
                    }
                }
            }
            return best;
        }

        private static IEnumerable<string> Variants(string catalogName)
        {
            string[] parts = catalogName.Split('/');
            string first = Normalize(parts[0]);
            yield return first;
            int lastSpace = first.LastIndexOf(' ');
            string prefix = lastSpace < 0 ? "" : first.Substring(0, lastSpace + 1);
            string lastToken = first.Substring(lastSpace + 1);
            int dash = lastToken.IndexOf('-');
            string tokenPrefix = dash < 0 ? "" : lastToken.Substring(0, dash + 1);
            for (int i = 1; i < parts.Length; i++)
            {
                string alternative = Normalize(parts[i]);
                if (alternative.Length == 0) continue;
                yield return alternative.IndexOf(' ') >= 0 ? alternative : prefix + tokenPrefix + alternative;
            }
        }

        private static string Normalize(string name)
        {
            string cleaned = Noise.Replace(name.ToLowerInvariant(), " ");
            return Spaces.Replace(cleaned, " ").Trim();
        }
    }
}
