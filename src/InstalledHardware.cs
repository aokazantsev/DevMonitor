using System;
using System.Collections.Generic;
using System.Security;
using Microsoft.Win32;

namespace DevMonitor
{
    internal static class InstalledHardware
    {
        private const string ProcessorKey = @"HARDWARE\DESCRIPTION\System\CentralProcessor\0";
        private const string DisplayAdaptersKey = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

        public static string ProcessorName()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(ProcessorKey))
                {
                    object value = key == null ? null : key.GetValue("ProcessorNameString");
                    return value == null ? null : value.ToString().Trim();
                }
            }
            catch (SecurityException)
            {
                return null;
            }
        }

        public static List<string> DisplayAdapterNames()
        {
            var names = new List<string>();
            try
            {
                using (RegistryKey adapters = Registry.LocalMachine.OpenSubKey(DisplayAdaptersKey))
                {
                    if (adapters == null) return names;
                    foreach (string subKeyName in adapters.GetSubKeyNames())
                    {
                        using (RegistryKey adapter = OpenSubKeySafely(adapters, subKeyName))
                        {
                            object value = adapter == null ? null : adapter.GetValue("DriverDesc");
                            if (value != null && !names.Contains(value.ToString())) names.Add(value.ToString());
                        }
                    }
                }
            }
            catch (SecurityException)
            {
            }
            return names;
        }

        private static RegistryKey OpenSubKeySafely(RegistryKey parent, string name)
        {
            try
            {
                return parent.OpenSubKey(name);
            }
            catch (SecurityException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }
    }
}
